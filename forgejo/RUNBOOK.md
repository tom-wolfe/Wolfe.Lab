# Forgejo runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap

Order matters — the 1Password item must exist **before** the merge, or
the deploy fails resolving it and the workflow goes red.

1. **Mint an auth key**: admin console → Settings → Keys → Auth keys →
   Generate. Not reusable, not ephemeral, no tags. Its own expiry barely
   matters — it's spent at enrolment and never replayed (`TS_AUTH_ONCE`).
2. **Vault it**: item `forgejo-tailscale` in the Wolfe.Lab vault, key in
   `credential`.
3. **Merge.** The forgejo workflow converges the stack and the sidecar
   enrols. Confirm the `forgejo` node in the admin console, then
   **disable its key expiry** — same reasoning as the mini itself: a
   server whose node key silently expires drops off the tailnet with
   nothing to notice.
4. **Declare the record.** Read the sidecar's address (admin console, or
   `tailscale status | grep forgejo` from any machine), write it in as
   the **default** of `forgejo_tailscale_ipv4` in `tofu/variables.tf`
   (caddy's `lab_tailscale_ipv4` pattern) and commit; then from the repo
   root: `lab deploy` from `forgejo/tofu`. Tripwire: **1 to add**
   (the `git.twolfe.dev` record), 0 changed, 0 destroyed. Until the
   default is written in, every plan of this root prompts for the
   variable — deliberate: it means the record isn't real yet.
5. **Repoint remotes** on each machine — the old
   `ssh://git@macmini.local:2222/...` URLs died with the port publish:

   ```sh
   old="ssh://git@macmini.local:2222/"
   new="git@git.twolfe.dev:"
   find ~/Development -maxdepth 4 -type d -name .git 2>/dev/null | while read -r g; do
     repo="${g%/.git}"
     url="$(git -C "$repo" remote get-url origin 2>/dev/null)" || continue
     case "$url" in
       "$old"*) git -C "$repo" remote set-url origin "$new${url#"$old"}"
                echo "repointed: $repo" ;;
     esac
   done
   ```

6. First contact from each machine accepts a new `known_hosts` entry.
   The host keys themselves are unchanged (same `data/ssh/`) — only the
   name is new.

## Upgrading

State lives in `data/`, so upgrades are a tag bump. **Back up first** — an
upgrade runs irreversible database migrations, and rolling back to an older
image after that will fail.

```sh
cd forgejo/backup && lab backup && cd ../..   # snapshot first
# bump the image tag in compose.yaml (normal PR; the push deploys it), then
# either let the forgejo compose workflow converge it or, by hand:
cd ~/.local/share/Wolfe.Lab/forgejo
docker compose pull
docker compose up -d
docker compose logs -f               # watch migrations complete
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

Runs itself: `.forgejo/workflows/forgejo-backup.yaml` snapshots this
slice nightly at 02:25 and drills the restore straight after
(`backup/ritten.json` declares what). Manual snapshot — run the backup workflow
from the Actions tab, or:

```sh
cd forgejo/backup
lab backup
```

Writes a snapshot into the restic repo on `/Volumes/Data2` (the image tag
rides on it as a snapshot tag; `restic-offsite.yaml` ships it to B2 and
owns retention — see `restic/README.md`). It refuses to run if the drive
isn't mounted — an unmounted `/Volumes` path on macOS silently writes to
the internal disk. It stops the container first — a live SQLite file
copied mid-write can be inconsistent — and starts it again afterwards, so
expect ~30s of downtime. If a concurrent deploy restarts the stack
mid-snapshot, the snapshot is discarded and the run fails loudly rather
than keeping a suspect copy.

Because `data/` is an ordinary folder, Time Machine already covers it too, but
only the cold-copy caveat above makes those snapshots trustworthy; prefer
the backup script before anything risky.

## Restore

```sh
cd forgejo/backup
lab restore
```

`restic/RUNBOOK.md` "Restore" for what it does and its options. The
image check matters here more than anywhere: Forgejo refuses to start
against a newer schema, so the job refuses to restore onto an image
other than the snapshot's.

## One-time setup

1. Create the 1Password items named in `tofu/secrets.env`:
   - `forgejo-api-token` — Forgejo -> Settings -> Applications -> Generate
     Token (read/write repository scope).
   - `github-tom-wolfe-pat`, `github-nschema-org-pat`,
     `github-disastercare-pat` — fine-grained, Contents: read-only, resource
     owner = that account/org, all (or selected private) repos. Orgs must
     allow fine-grained PATs (org Settings -> Personal access tokens).
   - `GitHub PAT Wolfe.Lab push` — fine-grained, Contents: read/write,
     scoped to tom-wolfe/Wolfe.Lab ONLY (this one can write; keep it narrow).

2. Delete the hand-made mirrors (they'd 409 against tofu's creates; mirrors
   are cattle — tofu recreates all of them uniformly):

   ```sh
   export FORGEJO_TOKEN=...   # or op read op://Wolfe.Lab/forgejo-api-token/credential
   curl -s -H "Authorization: token $FORGEJO_TOKEN" \
     'http://macmini.local:3000/api/v1/users/tom-wolfe/repos?limit=50' \
     | jq -r '.[] | select(.mirror) | .name' \
     | xargs -I{} curl -s -X DELETE -H "Authorization: token $FORGEJO_TOKEN" \
         "http://macmini.local:3000/api/v1/repos/tom-wolfe/{}"
   ```

3. `lab check` from `forgejo/tofu` — init, then a plan to read before applying.

## Bringing up the Pi (runbook, at the desk)

The Pi is already a chezmoi machine (profile `pi-node`, source cloned
from this instance over its deploy key). In order:

1. **Register server-side**: mint the vault item, then `lab register --node wolfe-pi5`
   from `forgejo/runners/` on the mini (or the `forgejo runners` workflow) — "The mini's runner" below has both halves.
2. **Binaries first** (Pi): `chezmoi git pull` (apply does not pull), then
   `chezmoi apply --include externals` — the registration template needs
   `op` to exist before it can render. It prints nothing on success; check
   `ls -l ~/.local/bin/forgejo-runner ~/.local/bin/op`. VERIFY:
   that chezmoi does not evaluate excluded templates on this path; if it
   does, fetch the two binaries by hand from the URLs in
   `.chezmoiexternal.toml.tmpl` once.
3. **Seed the bootstrap secret** (Pi):
   `install -D -m 600 /dev/null ~/.config/forgejo-runner/env` and write
   `OP_SERVICE_ACCOUNT_TOKEN=…` into it (the same service account the mini
   uses, or a Pi-specific one). Then `export OP_SERVICE_ACCOUNT_TOKEN=…` in
   the shell for the first apply.
4. **Lingering**, so the user service starts at boot with nobody logged in:
   `loginctl enable-linger tomwolfe` (may need sudo).
5. **Docker access**: the runner runs as the login user, so `id` must show
   the `docker` group (`sudo usermod -aG docker tomwolfe`, re-login).
6. **Apply**: `chezmoi apply` — renders config + registration, installs the
   units, and the `run_after` script enables and starts them. Do this
   BEFORE the first workflow runs on the node: `actions/checkout` needs
   node, and node arrives with this apply (a workflow cannot install its own checkout's prerequisite).
   `systemctl --user status forgejo-runner` should show it polling; the
   runner appears under the repo's Settings → Actions → Runners as
   `wolfe-pi5`, label `wolfe-pi5:host`, idle.
7. **The containerized runner** on the same node is the same two halves:
   `lab register --node wolfe-pi5 --kind docker` from `forgejo/runners/` on the mini
   (vault item `forgejo-runner-wolfe-pi5-docker`, instance scope, label
   `docker:docker://node:22-bookworm`), then on the Pi delete nothing and
   `chezmoi apply` — its config and registration render beside the host
   runner's under `~/.config/forgejo-runner-docker/`, and the `run_after`
   script starts `forgejo-runner-docker.service`. Both runners share one
   binary and one restart template; the two configs come from one partial
   in `.chezmoitemplates/forgejo-runner/`.
8. **First run**: Actions → "chezmoi" → Run workflow. Green =
   `chezmoi update` ran on the Pi from Forgejo. Then break it on purpose
   (e.g. a bad command via `workflow_dispatch` on a branch) to see the
   Pushover alert.

Things this design assumes and the first run should prove: `%h` resolves
in the path unit's `PathChanged=` lines (`systemctl --user cat
forgejo-runner.path`); a restart triggered mid-tick lets the running job
finish (`KillMode=mixed` so only the runner is signalled, and
`TimeoutStopSec` > `shutdown_timeout` — the default kill mode signals the
job's processes too, and a restart mid-job killed the job); host-mode steps
inherit the runner's `envs` PATH (chezmoi/op/docker resolve); the deploy key
in `~/.ssh/config` lets `chezmoi update` pull headless; `code.twolfe.dev`
resolves and its certificate validates from the Pi over the tailnet; and a
`pull_request` workflow naming `wolfe-pi5` will run there too — the trust
note in ROADMAP.md.

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
3. Redeploy — merge, or `lab deploy` from the slice in a checkout.

## After a reboot

Nothing to do. The mini signs in automatically and Docker Desktop starts
at sign-in, so every `restart: unless-stopped` container is back without
a hand. Both are macOS settings, not files (System Settings → Users &
Groups → automatic login; Docker Desktop → Settings → General → start at
sign-in), so a fresh mini needs them set once. Verify: `docker ps` over
SSH after a power cut, and the heartbeat check going green on its own.

## Restarting a runner

A runner reads `config.yaml` — the job PATH and envs included — only at
start, so a config change is not live until the runner is.

- **Pi**: nothing to do. `forgejo-runner.path` watches the config,
  registration and unit file and restarts the service when chezmoi
  writes any of them. By hand: `systemctl --user restart forgejo-runner`.
- **Mini**: launchd watches nothing, so after a `chezmoi` run that
  touched `~/.config/forgejo-runner/`:

  ```sh
  launchctl kickstart -k gui/$(id -u)/dev.twolfe.forgejo-runner
  ```

  `-k` stops the running instance first; a job in flight gets the
  runner's `shutdown_timeout` (10 min, under the agent's `ExitTimeOut`)
  to finish, so pick a moment when nothing long is running, or accept
  the wait. Confirm with `launchctl print gui/$(id -u)/dev.twolfe.forgejo-runner | grep state`
  and the runner going idle again under Settings → Actions → Runners.

## The mini's runner

1. Mint the secret and register. The mini's service account is read-only,
   so the item is created from the laptop, once; the registration runs
   wherever the forgejo container is, and is safe to repeat:

       secret=$(ssh macmini.local docker exec -u git forgejo forgejo forgejo-cli actions generate-secret)
       op item create --category "API Credential" --vault Wolfe.Lab --title forgejo-runner-MacMini "credential=$secret"
       cd forgejo/runners
       lab register --node MacMini

   A containerised runner is the same with `--kind docker` and the item
   `forgejo-runner-<node>-docker`.
2. Seed the bootstrap env: `install -D -m 600 /dev/null
   ~/.config/forgejo-runner/env` and write
   `OP_SERVICE_ACCOUNT_TOKEN=$(cat ~/Docker/1password/service-account-token)`.
3. `chezmoi apply` installs the formula via the Brewfile, renders the
   config and registration, and places the launchd agent
   `dev.twolfe.forgejo-runner` (`KeepAlive`, so it retries until Forgejo
   answers). Load it once: `launchctl bootstrap gui/$(id -u)
   ~/Library/LaunchAgents/dev.twolfe.forgejo-runner.plist`. Logs:
   `~/Library/Logs/forgejo-runner.log`.
4. The runner shows idle under Settings → Actions → Runners as `MacMini`.
   Run the `beszel compose` workflow by hand: green = the component was installed under
   `~/.local/share/Wolfe.Lab` and a compose deploy ran on the mini from
   Forgejo, through the headless Docker config.
