# Forgejo — home server

Self-hosted git server running on the Mac mini via Docker Compose, and the
source of truth for every repository in the lab (push-mirrored to GitHub).

| | |
|---|---|
| Web UI | http://macmini.local:3000 (also http://192.168.0.8:3000) |
| SSH clone port | 2222 |
| Image | `codeberg.org/forgejo/forgejo` pinned in `compose.yaml` |
| State | `~/Docker/forgejo/data` (bind mount → `/data` in the container) |
| Database | SQLite at `~/Docker/forgejo/data/gitea/forgejo.db` |
| Backups | `/Volumes/Data1/backups/forgejo` (external drive; nightly) |

This slice holds everything forgejo-shaped:

- `compose.yaml` — the stack definition (no secrets)
- `flows/` — the deploy pipeline and the nightly backup (`flow.yaml` + `script.sh` each, below)
- `tofu/` — every repository as code (below)
- `scripts/` — utilities (`import.sh`, the old-repo importer)

**`~/Docker/forgejo/data`** holds everything stateful — repos, `app.ini`, the
SQLite DB, SSH host private keys, JWT signing keys. It lives *outside* the
git repo on purpose: those are secrets, and they'd otherwise be one
`git add -A` away from being committed. Set `FORGEJO_DATA` to relocate it
(both `compose.yaml` and `backup.sh` honour it). Nothing is stored in the
container itself, so the image can be replaced freely.

## Deployment

The `deploy-forgejo` flow in Kestra converges this stack after every green
`chezmoi-update` tick — merge a compose change and it lands within one tick
(≤15 min). Manual converge: run the flow from the Kestra UI, or on the mini:

```sh
"$(chezmoi source-path)/../../forgejo/flows/deploy/script.sh"
```

## Day-to-day

```sh
cd "$(chezmoi source-path)/../../forgejo"

docker-compose ps            # status
docker-compose logs -f       # follow logs
docker-compose restart       # restart
docker-compose down          # stop (data is untouched)
docker-compose up -d         # start
```

`restart: unless-stopped` brings the container back automatically — **but only
once the Docker daemon is running.** See "After a reboot" below.

## First-run setup

On a fresh data directory, http://macmini.local:3000 shows the installer with
the database and URL fields pre-filled from `compose.yaml`. Leave them alone and
submit — the admin fields at the bottom are optional, and the **first account to
register becomes an admin** either way.

> **Never put `INSTALL_LOCK` in `compose.yaml`.** It is one-time state written
> by the installer, not configuration. Because `environment-to-ini` re-applies
> every `FORGEJO__*` variable on each start, pinning it there reverts what the
> installer just saved, and Forgejo aborts fatally on the next boot. See
> Troubleshooting.

Then lock it down, since this is a LAN server that doesn't need public signups:

1. Register your account at http://macmini.local:3000/user/sign_up
2. Set `FORGEJO__service__DISABLE_REGISTRATION: "true"` in `compose.yaml`
   (this one *is* safe to manage via env — it's ordinary config, and setting it
   is idempotent)
3. `docker-compose up -d`

## Where the admin password lives

In the SQLite DB inside `~/Docker/forgejo/data` — not in any env file. So
recreating the *container* never touches it, and restoring a backup restores
it. Keep the canonical copy in 1Password (an ordinary Login item) and treat
the DB as the cache: if they ever diverge (say, a from-scratch reinstall
where you typed something new), resync the DB **to** the vault, not the
vault to the DB:

```sh
docker exec forgejo forgejo admin user change-password \
  --username tom-wolfe --password "$(op read 'op://Wolfe.Lab/<your forgejo login item>/password')"
```

(GUI session, since `op` needs it; check `--help` first if the image has
moved — this is the recovery path, not something that runs routinely.)

Set `FORGEJO__service__REQUIRE_SIGNIN_VIEW: "true"` as well if you don't want
repos browsable by anyone on the LAN without logging in.

## Cloning

HTTP:

```sh
git clone http://macmini.local:3000/<user>/<repo>.git
```

SSH — add your public key in the web UI under *Settings → SSH / GPG Keys*, then:

```sh
git clone ssh://git@macmini.local:2222/<user>/<repo>.git
```

The non-standard port is easy to forget. Put this in `~/.ssh/config` and plain
`git@macmini.local:user/repo.git` works:

```
Host macmini.local
    Port 2222
    User git
```

## Upgrading

State lives in `data/`, so upgrades are a tag bump. **Back up first** — an
upgrade runs irreversible database migrations, and rolling back to an older
image after that will fail.

```sh
"$(chezmoi source-path)/../../forgejo/flows/backup/script.sh"   # snapshot first
# bump the image tag in compose.yaml (normal PR; the tick ships it), then
# either let deploy-forgejo converge it or, by hand:
cd "$(chezmoi source-path)/../../forgejo"
docker-compose pull
docker-compose up -d
docker-compose logs -f               # watch migrations complete
```

Rules worth respecting:

- **Never skip a major version.** To go 16 → 18, stop at 17 first, let it start
  and finish migrating, then move on.
- Read the release notes for major bumps: https://codeberg.org/forgejo/forgejo/releases
- If it won't start after an upgrade, restore the backup (below) and pin the
  old tag again.

Checking what's current:

```sh
curl -s "https://codeberg.org/api/v1/repos/forgejo/forgejo/releases?limit=5" \
  | grep -o '"tag_name":"[^"]*"'
```

## Backup

Runs itself: the `backup-forgejo` flow fires nightly at 03:05
(`flows/backup/`). Manual snapshot — run the flow from the Kestra UI, or:

```sh
"$(chezmoi source-path)/../../forgejo/flows/backup/script.sh"
```

Writes a timestamped tarball to `/Volumes/Data1/backups/forgejo/` (with the
image tag recorded beside it in `.image.txt`) and keeps the last 10. It
refuses to run if `Data1` isn't mounted — an unmounted `/Volumes` path on
macOS silently writes to the internal disk. It stops the container first —
a live SQLite file copied mid-write can be inconsistent — and starts it
again afterwards, so expect ~30s of downtime. If a concurrent deploy
restarts the stack mid-tar, the archive is discarded and the run fails
loudly rather than keeping a suspect copy.

Because `data/` is an ordinary folder, Time Machine already covers it too, but
only the cold-copy caveat above makes those snapshots trustworthy; prefer
the backup script before anything risky.

### Restore

```sh
cd "$(chezmoi source-path)/../../forgejo"
docker-compose down
rm -rf ~/Docker/forgejo/data                   # or move it aside first
tar -xzf /Volumes/Data1/backups/forgejo/forgejo-YYYYmmdd-HHMMSS.tar.gz \
    -C ~/Docker/forgejo
docker-compose up -d
```

Make sure the image tag in `compose.yaml` matches the version the backup was
taken with (recorded beside each archive in `.image.txt`), or Forgejo may
refuse to start against an older schema.

## After a reboot

Docker Desktop is **not** currently set to launch at login, so nothing starts
itself after a restart. Fix it once:

*Docker Desktop → Settings → General → tick "Start Docker Desktop when you sign
in" → Apply & restart.*

Until that's on, after every reboot you need:

```sh
open -a Docker && "$(chezmoi source-path)/../../forgejo/flows/deploy/script.sh"
```

Note this is tied to **signing in**, not to boot — a Mac mini sitting at the
login screen after a power cut won't run Forgejo. If that matters, enable
automatic login in System Settings → Users & Groups.

## Troubleshooting

**Clicking Install gives `ERR_CONNECTION_RESET` and returns you to the install
page.** Caused by `FORGEJO__security__INSTALL_LOCK` being set in `compose.yaml`.
The sequence:

1. Install writes `INSTALL_LOCK = true` to `app.ini`, then restarts
2. The image's `environment-to-ini` (`/etc/s6/gitea/setup`) re-stamps every
   `FORGEJO__*` variable over `app.ini`, reverting it to `false`
3. Forgejo sees an installed database with an unlocked install and dies:
   `MustInstalled() [F] Unable to load config file for a installed Forgejo
   instance` — the process drops mid-request, hence the connection reset
4. It restarts onto the install page, so the attempt looks like a no-op

Fix: remove `INSTALL_LOCK` from `compose.yaml` entirely and let `app.ini` own it.
The general rule — anything the installer or the app writes to `app.ini` as
*state* must not be pinned via `FORGEJO__*`, because those variables win on
every boot.

```sh
docker exec forgejo grep -i INSTALL_LOCK /data/gitea/conf/app.ini   # want: true
docker-compose logs | grep MustInstalled
```

**Container restart-loops with `bind: address already in use` on port 22.**
The image runs its own OpenSSH daemon as an s6 service (`/etc/s6/openssh/run`),
which always binds container port 22 and can't be disabled by configuration.
Setting `FORGEJO__server__START_SSH_SERVER: "true"` starts a *second* SSH server
on that same port; whichever loses the race dies, and when it's Forgejo's the
whole container exits. Fix: don't set `START_SSH_SERVER` at all. `SSH_PORT` only
controls the port advertised in clone URLs.

Note that a passing healthcheck does **not** rule this out — the check only
probes HTTP, so read the logs when diagnosing:

```sh
docker-compose logs | grep -iE 'address already in use|\[F\]|fatal'
docker inspect forgejo --format '{{.RestartCount}}'   # >0 and climbing = looping
```

## Repositories as code (`tofu/`)

Declaratively manages every repository on the Forgejo instance. State lives
in Garage (`s3://tofu-state/forgejo/terraform.tfstate`).

### The three repo shapes

| Shape | How | Meaning |
| --- | --- | --- |
| `mode = "mirror"` | pull mirror | Read-only copy, synced from GitHub every 8h |
| `mode = "active"` | one-time clone | Writable working fork; mirroring switched off |
| primary | `primary.tf` | Wolfe.Lab: Forgejo is the source of truth, push-mirrored to GitHub on every commit |

**Adding a repo** = one line in the `repos` map in `mirrors.tf`.

**Switching modes** = edit the `mode` value — but read this first:
Forgejo cannot convert between mirror and regular in place, so a mode flip
**replaces** the repository (tofu will plan a destroy + create):

- `mirror -> active`: safe — the mirror is destroyed and a fresh writable
  clone is taken from GitHub.
- `active -> mirror`: **destroys the active Forgejo copy, including any
  work not pushed elsewhere.** Push first, flip second. The plan output
  shows the replacement; treat `-/+ forgejo_repository` as a red flag to
  double-check.

### One-time setup

1. Create the 1Password items named in `tofu/secrets.env`:
   - `Forgejo API Token` — Forgejo -> Settings -> Applications -> Generate
     Token (read/write repository scope).
   - `GitHub PAT tom-wolfe`, `GitHub PAT nschema-org`,
     `GitHub PAT DisasterCare` — fine-grained, Contents: read-only, resource
     owner = that account/org, all (or selected private) repos. Orgs must
     allow fine-grained PATs (org Settings -> Personal access tokens).
   - `GitHub PAT Wolfe.Lab push` — fine-grained, Contents: read/write,
     scoped to tom-wolfe/Wolfe.Lab ONLY (this one can write; keep it narrow).

2. Delete the hand-made mirrors (they'd 409 against tofu's creates; mirrors
   are cattle — tofu recreates all of them uniformly):

   ```sh
   export FORGEJO_TOKEN=...   # or op read
   curl -s -H "Authorization: token $FORGEJO_TOKEN" \
     'http://macmini.local:3000/api/v1/users/tom-wolfe/repos?limit=50' \
     | jq -r '.[] | select(.mirror) | .name' \
     | xargs -I{} curl -s -X DELETE -H "Authorization: token $FORGEJO_TOKEN" \
         "http://macmini.local:3000/api/v1/repos/tom-wolfe/{}"
   ```

3. `cd tofu && op run --env-file=secrets.env -- tofu init`

### Day-to-day

```sh
cd tofu
op run --env-file=secrets.env -- tofu plan
op run --env-file=secrets.env -- tofu apply
```

State inspection: `op run --env-file=secrets.env -- tofu state list`.

### Notes

- The push mirror keeps the GitHub copy of Wolfe.Lab current on every
  commit, so everything that pulls from GitHub (the mini's chezmoi, the
  bootstrap one-liner, deploy keys) keeps working unchanged.
- Private mirrors store their PAT inside Forgejo per-repo, but Forgejo only
  consumes it at migration time — there is no API to update mirror
  credentials, so a `tofu apply` after rotating a PAT "succeeds" while the
  mirror keeps pulling with the dead token (2026-08-25). To rotate: update
  1Password, then set the new token in each repo's Settings -> Mirror
  Settings -> Authorization in the Forgejo UI, then run `tofu apply` once so
  state catches up (that apply is a harmless server-side no-op).
- No state locking (Garage lacks conditional writes): one operator, one
  machine at a time.

### Known provider issues (svalabs/forgejo 1.6.0)

- **Perpetual in-place "changes" on every repo** — the provider re-plans the
  computed `internal_tracker` block as unknown on every run (upstream #132,
  #169). Suppressed with `lifecycle.ignore_changes` in
  `mirrors.tf`/`primary.tf`; plans are clean now. Remove the workaround once
  fixed upstream.
- **Any update 500s on repos with wikis** ("'' is not a valid branch name")
  — the provider PATCHes the full repo object including an empty
  `wiki_branch`, which Forgejo treats as a branch rename to `""`. This hits
  *real* changes too, not just the no-op ones (seen flipping the feature
  units off on Hamelin, 2026-08-27). Workaround: make the same change
  out-of-band with a minimal PATCH that omits `wiki_branch` —

  ```sh
  curl -X PATCH -H "Authorization: token $(op read 'op://Wolfe.Lab/Forgejo API Token/credential')" \
    -H 'Content-Type: application/json' -d '{"has_actions":false}' \
    http://macmini.local:3000/api/v1/repos/<owner>/<name>
  ```

  — then `tofu apply`: refresh sees reality matching config, clean no-op
  (the same recover-by-hand-then-apply pattern as PAT rotation above).
- **Deleting a repo outside tofu breaks refresh** ("Repository with ID N not
  found") — intentional per upstream #111. Recover with
  `tofu state rm 'forgejo_repository.repo["<name>"]'`, then apply to
  recreate.
- **`auth_token` updates are silent no-ops** — the provider accepts the
  change but Forgejo has no API for it (see the rotation note above). It
  should arguably be flagged RequiresReplace; not yet reported upstream.
- **Creates sometimes error with "invalid result object" and drop the new
  repo from state.** The repo IS created on Forgejo; adopt it instead of
  retrying:
  `tofu import 'forgejo_repository.repo["<name>"]' '<forgejo-owner>/<name>'`
- The same crash can leave a resource **tainted**; if the repo is healthy,
  `tofu untaint 'forgejo_repository.repo["<name>"]'` rather than letting it
  replace.

The "invalid result object" crash and the `auth_token` no-op are still worth
upstream issues at svalabs/terraform-provider-forgejo.

## Notes

- Config is set through `FORGEJO__section__KEY` environment variables rather
  than by hand-editing `data/gitea/conf/app.ini`. Those variables are written
  into `app.ini` on every container start, so `compose.yaml` stays the single
  source of truth and edits made directly to `app.ini` get overwritten.
- `ROOT_URL` must match the address you actually type in the browser. If you
  later add a domain or reverse proxy, update `FORGEJO__server__DOMAIN` and
  `FORGEJO__server__ROOT_URL` together, or generated clone links and redirects
  will point at the wrong host.
- Nothing here is exposed to the internet. The ports are published on all
  interfaces, so anything on the LAN can reach it, but no router port
  forwarding is configured.
