# Beszel

Host monitoring for the lab: CPU, memory, disk, network and temperature,
with history and threshold alerts. [Beszel](https://beszel.dev) is two
pieces — a **hub** (the dashboard and alerting engine, a container in this
slice) and an **agent** (the thing that actually reads the metrics, a
native process on each monitored machine).

| Concern | Handled by |
| --- | --- |
| Hub container | `.forgejo/workflows/beszel-compose.yaml` on every push that touches `compose/` (the mini's host runner); first bring-up via `setup.sh` |
| Hub state (`~/Docker/beszel/data`) | nightly cold backup, `beszel-backup.yaml` (below) |
| Agent binary (Macs) | declared in the Brewfile (`chezmoi/home/dot_Brewfile.tmpl`); upgraded by hand (see "Two pins") |
| Agent binary (Linux nodes) | pinned release in `chezmoi/home/.chezmoiexternal.toml.tmpl`, installed to `~/.local/bin` |
| Agent supervision and config | the `agent/` component: `agent/ritten.json` declares each node's agent — environment, vault references, log — and `.forgejo/workflows/beszel-agent.yaml` runs `lab deploy --node <node>` on every node's own runner, rendering a launchd agent on the Macs and a systemd user unit on the Pi (`dev.twolfe.beszel-agent`) |
| Hub liveness | Gatus, from the Pi (`gatus/config/lab.yaml`) — Beszel cannot alert about its own hub being down |
| Route (`beszel.twolfe.dev`) | `caddy.caddyfile`, imported by the front door |
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

The cost is supervision: a native process needs launchd on a Mac and
systemd on Linux. The lab declares it rather than scripting it — the
`agent/` component is an `agents` component, the same shape as ollama's
server, and `lab deploy` renders the platform's unit and converges it
(build/README.md "Agents"). It reports into a dashboard, so it is
observable, which was the actual requirement.

Two workflows, two halves: **`beszel compose` converges the hub, and
`beszel agent` converges the agent on every node.** The binary is still
a host package — Homebrew's on the Macs, chezmoi's pinned download on the
Pi — and only its supervision and config belong to the component.

## How the two halves find each other

The agent dials **out** to the hub over a WebSocket; nothing ever connects
in. Two values make that work, both vault references in
`agent/ritten.json`, resolved into the agent's unit at deploy — which is
why the unit is written for its owner alone:

- `TOKEN` — a *universal* registration token from the hub's
  `/settings/tokens`. Universal means the same value enrols any number of
  agents, so adding a server is a node in `agent/ritten.json`, not a new
  secret per machine.
- `KEY` — the hub's **public** key, which is how the agent decides the
  thing answering is really our hub.

`HUB_URL` is `http://localhost:8090`, not `https://beszel.twolfe.dev`,
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
into "host compromised" (the containerised Actions runner never mounts it
into a job for the same reason).

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

**Who watches the hub:** Gatus, from the Pi (`gatus/config/lab.yaml`
asks `/api/health` every two minutes, three failures page). This is the
failure Beszel structurally cannot report: a hub that isn't running sends
no alerts, and it looks exactly like a healthy lab, because the deploy is
a convergent no-op that stays green regardless. Gatus is on the other
machine, and Gatus itself is probed from the mini (`gatus-health.yaml`),
so the chain has no self-reference.

Note what that check is *not*: it isn't reading `compose.yaml`'s Docker
healthcheck. Docker healthchecks are inert in this lab — nothing reads them
and nothing alerts on them. Caddy's has failed nearly four thousand times in
a row in total silence. The check that alerts is the one that asks.

## The configuration that isn't code

Every other slice declares its API resources in tofu — buckets in
`garage/tofu`, repositories in `forgejo/tofu`, checks in `gatus/tofu`. This
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
value in the vault, the deploy resolves it onto the machine, and wiping
the machine brings the same value back.

Beszel inverts that. The hub mints both the token and its keypair on first
boot; the vault holds a **copy**. So deleting `~/Docker/beszel/data` does
not restore these values, it invalidates them — you re-harvest from the new
hub and update the vault, then run the beszel agent workflow so every node
picks the new pair up.

One item, Wolfe.Lab vault:

| Item | Fields | Notes |
| --- | --- | --- |
| `beszel-agent` | API Credential item: `credential` = universal token, `username` = the hub's public key | Both come from the hub's `/settings/tokens`. `username` holding a public key is the same field-reuse as the `pushover` item; the key is not secret, it lives here so the pair that's invalidated together is re-harvested together |

## Operational notes

- Agent logs: on the Macs `~/.cache/beszel/beszel-agent.log` (both
  streams); on the Pi, the journal — `journalctl --user -u
  dev.twolfe.beszel-agent`.
- Agent lifecycle: run the **beszel agent** workflow; it converges every
  node, restarting an agent only when its unit changed. By hand, on a node:
  `cd beszel/agent && lab deploy --node <node>`. State: `launchctl print
  gui/$(id -u)/dev.twolfe.beszel-agent` on a Mac, `systemctl --user status
  dev.twolfe.beszel-agent` on the Pi.
- Rotating the token/key: update the `beszel-agent` item and run the
  workflow — the values are resolved at deploy, so a changed one is a
  changed unit and a restart.
- A rehearsal (`--dry-run`) resolves the vault references too: it cannot
  tell whether the unit changed without them. At a desk that means the
  1Password app asks to approve it.
- Hub health by hand: `curl -s http://macmini.local:8090/api/health`.
- The hub's `:8090` publish is load-bearing, not a convenience — the agent
  uses it. Don't remove it when tidying ports.

## Backup

`lab backup` — the shared pipeline (`build/`); the `backup` section of
`ritten.json` declares the paths — stops the hub, snapshots `~/Docker/beszel/data`
into the restic repo on `/Volumes/Data2` (tagged with the image it was
taken under; `restic-offsite.yaml` ships it to B2 and owns retention — see
`restic/README.md`), and starts it again, refusing to run if the drive
isn't mounted. Nightly via `beszel-backup.yaml` at 02:20, the restore
drilled straight after.

The stop is not optional: PocketBase runs SQLite in WAL mode, and copying
that live can capture a database file without the `-wal` that completes it.
A few seconds of downtime costs a gap in one metrics series.

The agent has nothing to back up — its configuration is `agent/ritten.json`,
and 1Password holds the two values it references. Its fingerprint, in
`~/.config/beszel/fingerprint`, is regenerated and re-enrolled if lost.

## The Pi

The second host shape, declared beside the mini's in `agent/ritten.json`:
`HUB_URL` is the mini's MagicDNS name rather than `localhost`, no
`EXTRA_FILESYSTEMS` (the drives are the mini's), and `DOCKER_HOST` is the
Pi's own socket. The binary is a pinned release fetched by chezmoi into
`~/.local/bin`; `lab deploy` runs it as a systemd user unit, which needs
lingering on (`loginctl enable-linger`, the Pi's bootstrap) to run with
nobody logged in. Its output goes to the journal.

Container stats: the agent reads `/var/run/docker.sock` as the login
user, who is in the `docker` group. Same reasoning as on the mini — a
native process that already owns the socket gains nothing from reading it.

In the hub UI: thresholds, and **Status alerts ON** — this machine is
always-on and off is a failure. Take the SoC and NVMe temperature
baselines while you are there.

## The Studio

The mini's shape with the Pi's `HUB_URL`, since the hub is on another
machine. No `DOCKER_HOST`: the Studio runs no lab containers
(ROADMAP.md #7). Its deploy waits while it sleeps and runs when it wakes.

**Status alerts OFF.** The Studio is a hybrid node: off is its normal
state most of the day, and nothing depends on it being on. A down alert
would fire every evening and teach you to ignore Beszel. What it is
worth watching for is load while it is on — how much a background model
costs the desk — so set thresholds, and leave status alone.

## Moving off chezmoi and Homebrew's service

Until 0.44.0 the agent ran under `brew services` on the Macs and a
chezmoi-written unit on the Pi, reading `~/.config/beszel/beszel-agent.env`.
The component's agents *supersede* those — `sh.brew.beszel-agent`, and
`beszel-agent.service` with its `.path` — so the first deploy on each node
stops and removes the old unit before starting its own, and the hub sees
the same system (the fingerprint is unchanged). What it leaves behind is
the old env file, holding the token: `rm ~/.config/beszel/beszel-agent.env`
on each node once the new agent shows up in the hub.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
