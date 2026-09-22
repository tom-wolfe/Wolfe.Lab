# Forgejo — home server

Self-hosted git server running on the Mac mini via Docker Compose.

| | |
|---|---|
| Web UI | http://macmini.local:3000 (also http://192.168.0.8:3000) |
| SSH clone | `git@git.twolfe.dev:<user>/<repo>.git` — portless, tailnet-only |
| State | `~/Docker/forgejo/data` (bind mount → `/data` in the container) |
| Database | SQLite at `~/Docker/forgejo/data/gitea/forgejo.db` |
| Backups | restic repo on `/Volumes/Data2` + B2 offsite (nightly; `restic/README.md`) |

## Deployment

`.forgejo/workflows/forgejo.yaml` converges this stack on every push that touches it, on
the mini's host runner. Manual converge: run the workflow from the Actions
tab, or on the mini:

```sh
lab deploy
```

## Day-to-day

```sh
cd ~/.local/share/Wolfe.Lab/forgejo

docker compose ps            # status
docker compose logs -f       # follow logs
docker compose restart       # restart
docker compose down          # stop (data is untouched)
lab deploy                   # start — from a checkout: the deploy is what
                             # puts the sidecar's auth key in its environment
```

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

HTTPS — the URL the web UI prints (`ROOT_URL`), tailnet-routed through
caddy:

```sh
git clone https://code.twolfe.dev/<user>/<repo>.git
```

SSH — add your public key in the web UI under *Settings → SSH / GPG Keys*, then:

```sh
git clone git@git.twolfe.dev:<user>/<repo>.git
```

Portless, standard SSH, no `~/.ssh/config` tricks — the port-2222 era and
its config workaround are gone. The name resolves to the container's own
tailnet address (next section), so it works from any tailnet machine, on
the LAN or off it, and from nowhere else. `git@forgejo.tailf823b8.ts.net:`
reaches the same SSH endpoint by its DNS-independent MagicDNS name.

Non-tailnet fallback, LAN only, no TLS:

```sh
git clone http://macmini.local:3000/<user>/<repo>.git
```

## Tailnet identity

Forgejo is the one service with its own seat on the tailnet, provided by
a `tailscale/tailscale` sidecar sharing the forgejo container's network
namespace (`network_mode: service:server`) and advertised as
**`git.twolfe.dev`** (an A record in `tofu/records.tf` at the sidecar's
address). The entire point is one fact: on a dedicated address, container
port 22 is free, so clone URLs need no port. This is
`tailscale/README.md` follow-up #3, and the end of the wait recorded in
`compose.yaml`'s clone-URL decision.

Mechanics worth knowing (the rest is comments in `compose.yaml`):

- **Userspace mode.** No TUN device, no capabilities. tailscaled
  terminates inbound tailnet connections itself and proxies each to the
  same port on 127.0.0.1 of the shared namespace — forgejo's sshd on 22,
  and incidentally its web server on 3000. Nothing dials *out* through
  the sidecar, and userspace mode couldn't offer that to forgejo anyway.
- **Why `git.twolfe.dev` and not `code.twolfe.dev`.** One name
  resolves to one address, and the web name and the SSH name point at
  *different machines*: `code.twolfe.dev` must keep resolving to
  the mini so the web UI routes through caddy, and port 22 at the mini's
  address is macOS Remote Login — the very conflict that created :2222.
  (`code.twolfe.dev` has the same trap: the `*.ts` wildcard is
  also the mini.) So SSH gets its own name — the per-service "neat
  public name in the owning slice's tofu, pointing at a Tailscale IP"
  pattern from `caddy/README.md`, here in its first instance. The cost:
  resolution rides Netlify DNS, like every `.lab` name. The sidecar's
  MagicDNS name, `forgejo.tailf823b8.ts.net`, is the same endpoint with
  no DNS dependency — the fallback when Netlify is the broken thing.
- **And the web gets `code.twolfe.dev`**, the same
  pattern from the other side: a CNAME to `code.twolfe.dev` in
  `tofu/records.tf`, so it resolves to the *mini* and routes through
  caddy like the `.ts` name it aliases — it just doesn't look like a
  wildcard. It is `ROOT_URL`, so it is what the UI prints in every HTTPS
  clone box and link. Two names, two machines, one service: `code.` is
  the mini's caddy, `git.` is the sidecar's sshd.
- **State is disposable and not backed up.** `~/Docker/forgejo/tailscale`
  (node keys, tailnet IP) sits beside `data/`, not inside it, so the
  nightly backup ignores it on purpose. Wipe it and re-enrol with a
  fresh auth key: same MagicDNS name, new 100.x IP, and nothing
  references the IP.

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
docker compose logs | grep MustInstalled
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
docker compose logs | grep -iE 'address already in use|\[F\]|fatal'
docker inspect forgejo --format '{{.RestartCount}}'   # >0 and climbing = looping
```

**Tailnet SSH dead after a manual `docker restart forgejo`.** A restarted
container is a new network namespace, and the sidecar keeps the dead one —
qbittorrent's documented trap with the roles reversed. Compose-driven
recreates are covered (`depends_on.restart: true`), and the backup's
stop/start cycles both containers in dependency order; only a manual
restart of forgejo alone needs the chaser:

```sh
docker restart forgejo-tailscale
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

### Day-to-day

```sh
cd forgejo/tofu
dotnet run --project ../../build/src/Wolfe.Lab.Build -- check
dotnet run --project ../../build/src/Wolfe.Lab.Build -- deploy
```

Anything else against the root, with its secrets resolved:

```sh
cd forgejo/tofu
run="op run --env-file ../../build/tofu-state.env --env-file secrets.env --"
$run tofu state list
```

### Notes

- The push mirror keeps the GitHub copy of Wolfe.Lab current on every
  commit, so everything that pulls from GitHub (the mini's chezmoi, the
  bootstrap one-liner, deploy keys) keeps working unchanged.
- Private mirrors store their PAT inside Forgejo per-repo, but Forgejo only
  consumes it at migration time — there is no API to update mirror
  credentials, so a `tofu apply` after rotating a PAT "succeeds" while the
  mirror keeps pulling with the dead token. To rotate: update
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
  units off on Hamelin). Workaround: make the same change
  out-of-band with a minimal PATCH that omits `wiki_branch` —

  ```sh
  curl -X PATCH -H "Authorization: token $(op read 'op://Wolfe.Lab/forgejo-api-token/credential')" \
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

## Runners

Forgejo Actions is the lab's CD engine. The model: **one runner per node per trust level.** A
runner is one binary; what differs is the label *type*, which decides how a
job's steps execute:

| Runner | Label | Steps run | Scope | Environment | For |
|---|---|---|---|---|---|
| host | `<hostname>:host` | in a shell on the node, as the login user | **the lab repo only** | the lab's vault token (`env_file`); `LAB_ROOT`, where deploys install slices | chezmoi, deploys, backups — anything that mutates the node |
| containerized | `docker:docker://<image>` | in a fresh container per job, no host socket | **instance-wide** | none | the lab's CI (lint, plan-on-PR) and every other project: Ritten, NSchema, … |

Both are built (below); the containerized one runs on the Pi today: a
second process with its own config, registration and unit, no `env_file`,
capacity above one since builds are stateless, and a role label shared
across nodes so Forgejo can run a build wherever a node is idle. Deploys
stay pinned by hostname; builds float.

Two boundaries, and they are different things. **Runner scope** says which
repos may dispatch to a runner: the host runner answers only to
`tom-wolfe/Wolfe.Lab`, so no other repo's workflow can ever run in a shell
on a node. **The per-repo Actions flag** says which repos may run
workflows at all: `mirrors.tf` sets `has_actions` only for non-mirror
repos, so every pull mirror (including third-party code) is inert, and a
project joins Forgejo Actions only when its `mode` is deliberately flipped
to `active` — a planned replacement, reviewed like any other change.
Labels only route.

Actions itself is on by default in Forgejo and enabled on this repo
(`has_actions`); there is nothing to switch on server-side. Adding a node is
two halves:

**Server half — one command, on the mini, op signed in:**

```sh
cd forgejo
dotnet run --project ../build/src/Wolfe.Lab.Build -- register-runner --node wolfe-pi5
```

Mints a 40-hex secret with `forgejo-cli actions generate-secret`, stores it
as the vault item `forgejo-runner-<hostname>` (the origin), and registers
the host runner at **repository scope** (`tom-wolfe/Wolfe.Lab`) with the
label `<hostname>:host`. Re-running with the same secret updates the
runner in place. The optional second argument overrides the scope; the
containerized runner's registration will be instance-wide.

What the split buys, stated plainly: a host-mode job runs as the node's
user with the lab's vault token in its environment. Repository scope means
only workflow files in *this* repo can obtain that — including, still, a
workflow on a pull-request branch of this repo (the trust note in
ROADMAP.md). Other projects never see a shell on a node: they *build and
publish* (an image, a package) on the containerized runner, and the lab
*deploys* what they published through the slice's own workflow, the way it deploys any
other pinned image.

**Node half — chezmoi, the `pi-node` profile:**

| Source | Target | Role |
|---|---|---|
| `.chezmoiexternal.toml.tmpl` | `~/.local/bin/forgejo-runner`, `~/.local/bin/op` | pinned binaries, no sudo |
| `dot_config/forgejo-runner/config.yaml.tmpl` | `~/.config/forgejo-runner/config.yaml` | no secrets; re-renders on every apply |
| `dot_config/forgejo-runner/create_private_runner.json.tmpl` | `~/.config/forgejo-runner/runner.json` | registration, rendered ONCE from the vault item |
| `dot_config/systemd/user/forgejo-runner.service` | `~/.config/systemd/user/…` | the runner, as a user unit |
| `dot_config/systemd/user/forgejo-runner.path` | `~/.config/systemd/user/…` | systemd watches config, registration and unit; a change starts the shared `restart@forgejo-runner.service`. chezmoi only writes files — no change-detection script |
| `dot_config/systemd/user/restart@.service` | shared | one template unit that restarts whichever service a path unit names |
| `.chezmoiscripts/run_after_user-units.sh` | shared | after every apply: `daemon-reload`, then `enable --now` every non-template unit in the directory. Idempotent |

The registration file is the interesting part: Forgejo derives a runner's
UUID from the secret (`gouuid.FromBytes(secret[:16])` — the first sixteen
characters as raw bytes), so the template does the same arithmetic and the
node never needs `forgejo-runner register`. It is a `create_` file: `op` is
a bootstrap dependency, not a tick dependency, exactly like the env files.
The instance address is read from this slice's `compose.yaml` (`ROOT_URL`),
not typed again.

How a deploy job gets the repo: `actions/checkout`, like any pipeline,
into the job's workspace, which the runner disposes of afterwards. What
containers need to keep reading — config directories, route snippets —
is not read from the checkout: the deploy installs the slice into
`$LAB_ROOT/<slice>` (`~/.local/share/Wolfe.Lab`, rsync so nothing a
running container has open vanishes) and runs compose there. The front
door's hook gathers every slice's `caddy.caddyfile` into that tree, so
caddy's import glob is complete whatever runs on which node. Host runners
have node on the PATH only because `actions/checkout` is a JavaScript
action: on the mini a shim in `~/.local/share/forgejo-runner/bin` that
resolves nvm's default alias at call time (nvm owns the active version; a
brew node would fight it — the `nvm-run` pattern), on the Pi a pinned
external. chezmoi's own
source is a separate clone and no part of a deploy.

Secrets in jobs: **Forgejo holds none.** The runner's `env_file`
(`~/.config/forgejo-runner/env`, hand-seeded, mode 600) carries the node's
`OP_SERVICE_ACCOUNT_TOKEN`; jobs read the vault through the CLI's
secrets provider. One bootstrap secret per node, same as the mini.

### The mini's runner

Same host-runner design, macOS supervision. Forgejo publishes no macOS
binary, so the runner comes from Homebrew (`forgejo-runner` in the server
Brewfile), but launchd supervises it through a plist.

Bring-up is in `RUNBOOK.md` "The mini's runner".

Everything scheduled on the mini is a cron workflow on this runner:
each slice's `<slice>-backup.yaml`, `restic-offsite.yaml`,
`restic-verify.yaml`, the two `obsidian-*.yaml`, `caddy-renew-certs.yaml`,
`heartbeat.yaml` and `gatus-health.yaml`. Its capacity is 3 so the heartbeat, the syncs and the
probe never queue behind a long job; stateful jobs serialise through the
`MacMini` concurrency group.

## Notes

- Config is set through `FORGEJO__section__KEY` environment variables rather
  than by hand-editing `data/gitea/conf/app.ini`. Those variables are written
  into `app.ini` on every container start, so `compose.yaml` stays the single
  source of truth and edits made directly to `app.ini` get overwritten.
- `ROOT_URL` is `https://code.twolfe.dev/` — the address every generated
  link and HTTPS clone URL carries. The `.lab` and `.ts` names still
  route here and work in a browser; they just aren't what Forgejo prints.
  `FORGEJO__server__DOMAIN` and `FORGEJO__server__ROOT_URL` move
  together, or generated clone links and redirects point at the wrong
  host. Changing them is a container recreate (the tick-chained deploy
  does it); existing clones keep working, their remotes just print an
  older name.
- Nothing here is exposed to the internet. The `:3000` publish is on all
  interfaces, so anything on the LAN can reach the web UI, but no router
  port forwarding is configured; SSH is reachable only over the tailnet.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
