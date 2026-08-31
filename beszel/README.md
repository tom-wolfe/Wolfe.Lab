# Beszel

Host monitoring for the lab: CPU, memory, disk, network and temperature,
with history and threshold alerts. [Beszel](https://beszel.dev) is two
pieces — a **hub** (the dashboard and alerting engine, a container in this
slice) and an **agent** (the thing that actually reads the metrics, a
native process on each monitored machine).

Today it watches one machine, the mini. The laptops join when Tailscale
lands — see "Later: the laptops".

| Concern | Handled by |
| --- | --- |
| Hub container | the `lab.beszel/deploy` flow (`flows/deploy/flow.yaml`), chained on the chezmoi tick like every stack; first bring-up via `setup.sh` |
| Hub state (`~/Docker/beszel/data`) | nightly cold backup, `lab.beszel/backup` (below) |
| Agent binary | declared in the Brewfile (`chezmoi/home/dot_Brewfile.tmpl`, server section); installed by `lab.chezmoi/packages` and upgraded by `lab.chezmoi/packages-upgrade`, supervised by `brew services` |
| Agent config (`~/.config/beszel/beszel-agent.env`) | chezmoi `create_` template (`chezmoi/home/dot_config/beszel/`) — materialized from 1Password (`beszel-agent`) only while the file is missing |
| Hub liveness | the `lab.beszel/health` flow — Beszel cannot alert about its own hub being down |
| Route (`beszel.lab.twolfe.dev`) | `caddy.caddyfile`, imported by the front door |
| Systems, thresholds, notification URLs | **the hub's UI.** Not tofu — see "The configuration that isn't code" |

## Why the agent is a host process

This is the whole reason Beszel was chosen over a Prometheus stack, and
it is worth keeping straight, because "just run it in a container" is the
obvious-looking move and it silently produces useless numbers.

Docker Desktop on macOS runs containers inside a Linux VM. A container's
view of the filesystem is that VM's, not the mini's — so a containerised
exporter monitoring "disk" reports on a virtual disk nobody cares about.
`/Volumes/Data1`, the 1.8 TB drive holding most of the media library,
does not exist in there at all; bind-mounting it doesn't help either,
because VirtioFS mounts land in the default ignore list. Host metrics have
to come from a process on the host, and Beszel's agent is cross-platform
and runs natively on macOS. That's the pick.

The cost is supervision: a native process needs launchd, and `brew
services` is launchd. That's accepted, and it is consistent with what this
repo actually retired — Kestra replaced launchd's *scheduling*, not its
*supervision* (`kestra/README.md`). The objection to launchd was that
plists are opaque and there is nowhere to watch them. This agent reports
into a dashboard and shows up in `brew services list`, so it is observable,
which was the actual requirement.

Consequence worth internalising: **`lab.beszel/deploy` converges the hub
only.** The agent's lifecycle belongs to chezmoi, because the agent is a
host package and chezmoi is what converges host packages. One converger per
thing.

## How the two halves find each other

The agent dials **out** to the hub over a WebSocket; nothing ever connects
in. Two values make that work, both in `~/.config/beszel/beszel-agent.env`:

- `TOKEN` — a *universal* registration token from the hub's
  `/settings/tokens`. Universal means the same value enrols any number of
  agents, so adding the laptops later is a template change, not a new
  secret per machine.
- `KEY` — the hub's **public** key, which is how the agent decides the
  thing answering is really our hub.

`HUB_URL` is `http://localhost:8090`, not `https://beszel.lab.twolfe.dev`,
and deliberately: routing host monitoring through a public DNS name would
mean the lab stops watching itself the moment the internet goes down, which
is exactly when you want it watching. Names are for humans
(`caddy/README.md`); the hostname exists for the browser.

The legacy direction (hub connects to an agent listening on :45876) is not
used, so `LISTEN` is pinned to loopback rather than published to the LAN.

## What it watches

- **Disks** — the root volume plus `/Volumes/Data1` and `/Volumes/Data2`,
  named in `EXTRA_FILESYSTEMS`. This is where the roadmap's free-space
  problem gets solved: every backup script writes to Data1, retention
  prunes *after* the write, and none of them assert there is room first.
  One polling cycle with history and a threshold, in one place, rather than
  a fourth copy of a `df` check in a fourth script.
  **Read the limitation honestly**: a threshold alert *tells you* the drive
  is filling; it does not *stop* a backup that would fill it. It buys
  warning, not enforcement. That trade was made deliberately — an alert with
  a week of trend behind it is more useful than four guards that each fire
  at the moment it's already too late.
- **CPU, memory, swap, load, network.**
- **Containers** — per-container CPU/memory/network, and an alert when one
  stops. This is the agent reading Docker's socket, so read the note below
  before deciding it contradicts the lab's security model.
- **Temperature** — best-effort on this hardware. macOS has no equivalent of
  Linux's `hwmon`, and Apple Silicon sensor reads used to crash the agent
  outright ([#912](https://github.com/henrygd/beszel/issues/912), fixed
  mid-2025, on this exact machine class). Treat any temperature series as a
  bonus rather than something to build an alert you rely on.

### About the Docker socket

`DOCKER_HOST` points the agent at `~/.docker/run/docker.sock`. The socket is
Docker's control plane: anything that can talk to it can start a container
that mounts the whole disk, which is root access by another route. The rule
this lab follows is that **no container gets the socket** — that's the rule
that matters, because it's the one that turns "one container compromised"
into "host compromised", and Kestra in particular is built around avoiding
it (`kestra/README.md`).

The agent is a different shape. It is a native process already running as
`tomwolfe`, who owns the socket and can run `docker` at any prompt. Reading
it grants the agent nothing it could not already do, so the escalation the
rule exists to prevent is not on the table. If you ever containerise this
agent, that stops being true and the socket must come out.

## Alerting

Two independent paths, on purpose, because they fail differently.

**Beszel's own alerts** — thresholds on the metrics above, evaluated by the
hub, delivered through [shoutrrr](https://containrrr.dev/shoutrrr/). Point
it at the lab's existing Pushover application so these land in the same
place as everything else, using the `pushover` 1Password item:

```
pushover://shoutrrr:<credential>@<username>/
```

(`credential` is the application token, `username` the user key — same item
`system/alert-failed` uses.) Configured in **Settings → Notifications**, then
per-system thresholds in the systems table.

**`lab.beszel/health`** — a Kestra flow polling the hub's own `/api/health`
at 7/22/37/52 past the hour, labelled `alert: high`. This is the failure
Beszel structurally cannot report: a hub that isn't running sends no alerts,
and it looks exactly like a healthy lab, because `lab.beszel/deploy` is a
convergent no-op that stays green regardless. A monitoring tool nobody
monitors is the trap the roadmap's ordering principle exists to avoid, so
the hub gets a liveness probe on the day it arrives.

Note what that flow is *not*: it isn't reading `compose.yaml`'s Docker
healthcheck. Docker healthchecks are inert in this lab — nothing reads them
and nothing alerts on them. Caddy's has failed nearly four thousand times in
a row in total silence. The check that alerts is the one written in Kestra.

## The configuration that isn't code

Every other slice declares its API resources in tofu — buckets in
`garage/tofu`, repositories in `forgejo/tofu`, flows in `kestra/tofu`. This
one can't: Beszel has no Terraform/OpenTofu provider. Registered systems,
alert thresholds and notification URLs are clicked into the UI and live only
in `~/Docker/beszel/data`.

That is a genuine step down from the rest of the repo and it is worth naming
rather than hiding. Two consequences follow:

1. **The nightly backup is not optional.** It is the only copy of that
   configuration, which is why it's labelled `alert: high` even though the
   metrics history it also carries is expendable.
2. **Reproducing the hub from scratch is a manual ritual**, not an apply.
   Keep the bootstrap section below accurate; it is the runbook.

The hub is a PocketBase app, so a REST API does exist and this could be
scripted later. Not worth it for one machine and a handful of thresholds —
revisit if the fleet grows.

## Secrets: the hub is the origin (the one exception)

Everywhere else in this lab, 1Password is the *origin*: you create the
value in the vault, chezmoi caches it onto the machine, and wiping the
machine brings the same value back.

Beszel inverts that. The hub mints both the token and its keypair on first
boot; the vault holds a **copy**. So deleting `~/Docker/beszel/data` does
not restore these values, it invalidates them — you re-harvest from the new
hub and update the vault. The `create_` template says so at the top too.

One item, Wolfe.Lab vault:

| Item | Fields | Notes |
| --- | --- | --- |
| `beszel-agent` | API Credential item: `credential` = universal token, `username` = the hub's public key | Both come from the hub's `/settings/tokens`. `username` holding a public key is the same field-reuse as the `pushover` item; the key is not secret, it lives here so the pair that's invalidated together is re-harvested together |

## Bootstrap (one-time, in order)

The ordering is forced: the hub must exist before the token does, and the
token must be in the vault before the agent can start. Same chicken-and-egg
as `garage/tofu` — run once, harvest the outputs, store them.

> **Step 0 is not optional, and it happens BEFORE this branch merges.**
> Create the `beszel-agent` item in 1Password first, with placeholder values
> if you like — the real ones come from the hub in step 3, and step 4 is how
> you swap them in.
>
> Why it matters more than the usual "create the items first": a `create_`
> template is evaluated whenever its target is missing, and an
> `onepasswordRead` of an item that doesn't exist **fails the whole
> `chezmoi apply`**. On the mini that apply is the tick, a red tick chains
> to nothing, and every deploy flow in the lab stops until you fix it. That
> is the same shape as the 0.10.1 heartbeat incident. Merging this slice
> without the vault item present would take the lab's CD down, and the
> chicken-and-egg means you cannot recover it by simply re-running the tick.

1. **Bring the hub up.** `./setup.sh` from the repo root does it, or on the
   mini `docker compose --project-directory <this dir> up -d`. Caddy must
   already have been deployed once (it owns the `lab` network).
2. **Create the superuser.** First visit to http://macmini.local:8090
   prompts for it. Put it in 1Password as you would any login.
3. **Harvest the pair.** Settings → Tokens & Fingerprints. The universal
   token is **off by default** — enable it, and mark it **persistent**, or
   it expires after an hour and on every hub restart. Copy the token and
   the public key shown inline beside it into a `beszel-agent` API
   Credential item (`credential` and `username` respectively).

   Note what the token is: a *registration* token. Once an agent has
   enrolled it keeps working without it, so a stale vaulted token only
   bites when you enrol a new machine or wipe `~/.cache/beszel`. That's
   also why persistence matters — the laptops enrol months from now.
4. **Materialize the agent config.** On the mini, `chezmoi apply` — writes
   `~/.config/beszel/beszel-agent.env`. Needs `OP_SERVICE_ACCOUNT_TOKEN` in
   the environment for a non-login shell; `.zprofile` exports it on servers.
5. **Install and start the agent**: wait for `lab.chezmoi/packages` on the
   next tick, or by hand `brew bundle install --file=~/.Brewfile`. Confirm
   with `brew services list`.
6. **Confirm enrolment.** The mini should appear in the hub within a few
   seconds. If it doesn't, `tail ~/.cache/beszel/beszel-agent.log`.
7. **Configure notifications and thresholds** (see "Alerting"). Nothing
   alerts until you do — the hub ships no default thresholds.
8. **Register the flows**: `cd kestra/tofu` and
   `op run --env-file=secrets.env -- tofu apply`.

## Operational notes

- Agent logs: `~/.cache/beszel/beszel-agent.log` (both stdout and stderr —
  the formula points them at the same file).
- Agent lifecycle: `brew services {list,restart,stop} beszel-agent`. It
  reads its env file **at start only**, so any edit to
  `~/.config/beszel/beszel-agent.env` needs a restart to take effect.
- Rotating the token/key: update the `beszel-agent` item, delete
  `~/.config/beszel/beszel-agent.env`, `chezmoi apply`, restart the agent.
- Hub health by hand: `curl -s http://macmini.local:8090/api/health`.
- The hub's `:8090` publish is load-bearing, not a convenience — the agent
  uses it. Don't remove it when tidying ports.

## Upgrading

Two pins, and they should move together — the hub and agent speak a
versioned protocol and are only tested as a pair:

1. `image:` in `compose.yaml` (hub) — pinned, bumped by hand.
2. The agent has **no version to pin**: the tap ships a single
   `beszel-agent.rb` regenerated on each release, so there is nothing
   versioned to name in the Brewfile.

So the agent tracks upstream, on the nightly `lab.chezmoi/packages-upgrade`
schedule. That flow goes through `brew bundle` rather than plain `brew
upgrade` specifically for services like this one: `brew upgrade` replaces
the binary and leaves the old process running, whereas bundle honours
`restart_service: :changed` and restarts it. A monitor silently running a
stale binary is the exact failure worth avoiding here.

Consequence: the agent can move ahead of the hub's pinned image. That's
tolerable because a protocol mismatch is loud rather than silent — the mini
drops off the dashboard, and `lab.beszel/health` and
`~/.cache/beszel/beszel-agent.log` both say so. To hold it while you catch
up, `brew pin beszel-agent` on the mini; `brew upgrade` skips pinned
packages.

Bump the image via a normal PR; the tick ships it and `lab.beszel/deploy`
converges it. Check the release notes first — the hub migrates its SQLite
schema forward on boot and downgrades are not supported, so going back means
restoring a backup, which is why the backup records its image tag.

## Backup

`flows/backup/script.sh` stops the hub, tars `~/Docker/beszel/data` to
`/Volumes/Data2/backups/beszel/`, and starts it again — the last 10 kept,
each with the image tag it was taken under, refusing to run if the drive
isn't mounted. Nightly at 02:35 via `lab.beszel/backup`, staggered ahead of
garage's 02:50.

The stop is not optional: PocketBase runs SQLite in WAL mode, and tarring
that live can capture a database file without the `-wal` that completes it.
A few seconds of downtime costs a gap in one metrics series.

The agent has nothing to back up — its entire configuration is the
`create_` template's output, and 1Password holds what that's built from.

## Later: the laptops

Built 2026-08-31, once Tailscale landed — and it was exactly the small
change predicted: `.config/beszel` left the server-only branch of
`.chezmoiignore`, the Brewfile entry moved to the all-machines section,
and `HUB_URL` became machine-aware — `localhost:8090` on the mini,
`http://macmini.tailf823b8.ts.net:8090` on the laptops. The MagicDNS name
is deliberate: Tailscale resolves and routes it itself, at home or away,
so laptop monitoring depends only on the tailnet — never on public DNS or
the front door (the same reasoning as localhost on the mini). Same
universal token for every machine; no new vault item.

To enrol a laptop: `chezmoi apply` (materializes the env, installs and
starts the agent via the Brewfile), then watch it appear in the hub.
Then set its thresholds in the hub UI — and leave **Status alerts OFF**
for laptops: a machine that is allowed to sleep is not a failure, and a
Status alert would page on every lid-close (the same rule the roadmap
sets for the Studio). The token must be PERSISTENT in the hub's settings
or enrolment fails with a stale vault copy — see the template.
