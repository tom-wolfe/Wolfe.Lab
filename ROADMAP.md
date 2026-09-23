# Roadmap

What's coming and *why*, in rough order. `CHANGELOG.md` is the record of
what happened; this is the record of what we decided to do next and what we
decided to leave alone. Items move out of here into the changelog when they
ship.

Ordering principle: reduce risk before adding surface. Anything that makes
a failure visible outranks anything that adds a new thing to fail.

## Next

### 1. Restore drills — prove the backups before anything depends on them

A backup nobody has restored from has not shipped, so this is the first
item by the ordering principle: it makes the one failure that is silent
until it is total — an unrestorable backup — visible. The weekly half is
in place: each slice's `lab restore-drill` restores its `backup.verify` paths
from its latest snapshot and asserts them, and `lab restore` is the
restore itself. Honest scope: that proves the files come back, not that
the service boots on them.

**The boot-on-restore drill is manual and occasional.** Once a quarter:
pick a service and run `lab restore` on it, confirm it works, and delete
the `.bak` it set aside (or move it back). This is the one that catches
the "restored but the app doesn't like it" class, and it is what turns
the command into something someone has actually run in anger.

**A cold-start runbook belongs here too.** Mini and Data2 both gone:
what is needed from outside the building (the restic password, the tofu
state passphrase, the B2 credentials, the vault's own master password —
see the secret-zero note in #8), and in what order things come up
(garage before any tofu root, caddy before any route, Forgejo before any
workflow). Nothing in the repo says this today; `setup.sh` is the closest.
`lab restore` from the offsite repository is the missing piece of that
runbook: today both jobs read the local one.

### 2. Finish the *arr stack

Add Prowlarr and Jellyseerr to the existing Radarr/Sonarr/Jellyfin stack.

### 3. Renovate support

Add Renovate for things like outdated Docker images, and nuget packages.

### 6. CI, the build pool, and pipelines as a CLI

Every node runs a **host** runner for the lab's own CD: repo-scoped, host
mode, holding the node's vault token. The Pi also runs a **containerised**
runner: instance-scoped, the role label `docker`, no host environment and
no socket in a job, where CI runs today (`build.yaml`, and each slice's
`check` on every pull request) and where Ritten and NSchema will build and
publish once their repos flip from mirror to active. One thing remains:

- **The mini joins the build pool.** The containerised runner as a compose
  slice, identical on every node — the runner image, socket-mounted so it
  can start sibling job containers, config and registration rendered by
  chezmoi as now — deployed by each node's host runner. On the Pi that
  replaces the systemd-supervised second runner. One mechanical detail:
  a runner in a container hands job containers bind mounts by host path,
  so its work directory must be the same path inside and out; Forgejo's
  own compose example does exactly that.

### 7. The Mac Studio as a compute node

**Decided: a second node, not the mini's replacement, and a hybrid — a
workstation that also serves.** That is a change from workstation-only, and
it comes with one rule that keeps it safe: **the Studio may serve, but
nothing may depend on it.** A daily driver sleeps, reboots for updates and
gets fiddled with; this repo already refuses to let a watcher share the
fate of the thing it watches, and this is that principle one level up.
Anything the Studio provides must degrade rather than break.

**What it takes on.**

- **Immich's machine learning.** `immich/README.md` already anticipates
  this — Immich accepts a remote machine-learning URL and nothing else
  moves. The Studio asleep means search results go stale, not that Immich
  is down, which is exactly the degradation the rule asks for. The single
  best use of the hardware against what the lab already runs.
- **Ollama, as a host process, not a container.** A container on macOS gets
  no access to the Apple GPU, so a containerised model runs on CPU and is
  uselessly slow. This is the clearest correct case in the fleet for
  running on the host, and unified memory is the reason the hardware is
  worth anything here. `ollama` is already in the Brewfile's personal
  section, so this is a widening rather than a new dependency.
- **Whisper-family transcription**, local, for captured notes.

**What it does not take on:** anything in the platform layer, the drives,
or any job whose failure is an outage.

**What it needs structurally:** a chezmoi profile, Tailscale, a Beszel
agent, a Gatus check at a softer severity than a server's, and — as a
hybrid rather than a pure workstation — a host runner.

**The vault-querying front end.** Ollama plus a RAG layer over a clone of
the vault repositories on Forgejo (`obsidian/`), not a mount. The corpus is
already versioned and already synced by workflows, so the index job is an
ordinary `lab` job with a clean input.

### 8. A config plane — Garage for configuration, the Bitwarden exit for secrets

Two halves because they are the same move —
resolution goes LAN-local while every reference keeps its shape — and
they are separable builds, each landing when its own pressure arrives:
the Garage half on the SECOND cross-slice value, the secrets half once
the restore drills (#1) have passed — the offsite copy exists first and
is proven, then the origin moves.

**The config half.** The first cross-slice values are already here,
homed nowhere: the runner registration template reads Forgejo's
`ROOT_URL` out of `forgejo/compose/compose.yaml` with a cross-tree `include`, seven tofu roots carry the Garage
endpoint as a `macmini.local` literal, the tailnet suffix is typed into
fourteen files, and the mini's LAN address appears as two different IPs
(`caddy/tofu/variables.tf`, `forgejo/README.md`). Every one is retyped
by the Linux move (#2), which is the pressure this half was waiting
for. Hardcoding one slice's fact into another is the thing to refuse; a
consumer deriving it from the owning slice's files is only a consumer's
guess at a format that isn't its own.

The pattern: a dedicated `config` bucket
in Garage. The OWNING slice's tofu root publishes named values as S3
objects on its apply, so the publisher is never staler than the last
push. Consumers list-then-read (a list
tolerates absence where a read errors) and `count` the dependent
resource on presence, so a root deploying before its dependency simply
omits the wiring and picks it up on a later plan: partial deployment
breaks bootstrap cycles, and the daily plan's nag makes the eventual
consistency visible instead of silent. Two caveats, named at design
time. The failure mode INVERTS — absent config is a green plan that
deploys nothing, so every consumer carries a tofu `check` making absence
at least a named warning (the `heartbeat/tofu` pattern). And published
config is declared truth, not liveness — proving someone answers is
monitoring's job (`gatus/`), not this one's.

Why Garage and not the vault: configuration and secrets are different
jobs. 1Password is the origin of *secrets*, is a cloud round-trip —
deploys and jobs read it, nothing that runs does — and changing a value
there is a redeploy. Garage is LAN-local (every plan already polls it), sits below
every would-be publisher in the bootstrap order, and costs no new
service.

**The secrets half: leave 1Password for the Bitwarden ecosystem.**
Tom's decision, on ethical grounds, with the rate-limit outage as the
forcing function; a 1Password Connect cache would have solved the rate
limit but kept the vendor. Constraints fixed at decision time, so the design pass starts
from them rather than relitigating:

- **The origin stays in the cloud** (Bitwarden's), exactly as with
  1Password today. Any server-side piece is only ever a *replica or
  cache* — never the origin — because self-hosting the origin is the
  bootstrap/DR circularity OpenBao was deferred over, and it is a
  problem deliberately not taken on. Do not re-propose
  Vaultwarden-as-origin.
- **The human half is known-clean:** Bitwarden clients everywhere, and
  the desktop app's SSH agent (clients ≥2024.12) replaces 1Password's
  agent for the fleet.
- **The machine half is the actual design pass.** Vaultwarden cannot
  implement Secrets Manager (`bws` is not GPL-licensed), so unattended
  reads are either `bws` against Bitwarden cloud (the official machine
  accounts — check *its* rate limits before trusting it with the lesson
  of 0.16.0) or `bw`/`rbw` with a local cache — rbw's agent holds the
  vault locally, which is the Connect-shaped property: reads cost no
  quota and survive cloud outages. Ritten's `ISecretProvider` is already
  the one door every caller goes through, so the machine half is a second
  provider package plus the reference syntax in the env files — no caller
  changes;
  chezmoi has native `bitwarden`/`rbw` template functions for the
  `create_` files that remain (the runner registrations, the Beszel
  agent, the Pi's restic key).
- **Migration surface, inventoried:** every vault item, every env file
  of references (`secrets.env`, restic's, rclone's), the four `create_`
  files, the op service account (secret zero changes shape), the SSH
  agent on three Macs, the READMEs.

**Where the lab's secrets live in Bitwarden (Tom's plan).**
Bitwarden has no per-account vaults the way 1Password does. Its
equivalent is an *Organization* with *Collections*, which is worth
knowing for one reason: Secrets Manager's machine accounts (`bws`) are
an organization feature, so an org exists in any design that uses them.
Tom's plan is a **dedicated account** for the lab instead — a Bitwarden
account of its own, at a plus-address or catch-all on the domain
(Proton supports both; check the catch-all is on the plan). That is
the cleaner isolation whichever CLI the machine half ends up on: the
credential a server holds only ever unlocks the lab's items, and the
personal vault is never on a machine. The two are not exclusive — the
lab account can own an org if `bws` wins the design pass.

**The vault backup and the local replica — part of this migration,
not after it.** The restic repo password, the tofu
state passphrase and the B2 key exist only in the vault. Lose the
vault and every backup and every state file is unreadable, which makes
the vault the one thing in the lab with no backup of its own. Two
pieces, both riding the dedicated account:

- **A nightly encrypted export** — `bw export --format encrypted_json`
  with a *password-protected* export (the account-key-encrypted
  variant is only restorable into the same account, which is the wrong
  property for DR). A workflow, into a path restic already snapshots,
  so it reaches B2 with everything else. This is "sync it to a local
  vault by automation": a point-in-time replica that any Bitwarden
  client can import.
- **A live local cache for reads** — rbw's agent, or whatever the
  machine half settles on. This is the replica the flows read from when
  the cloud is unreachable. Vaultwarden is *not* an option for this
  role: it cannot sync from Bitwarden's cloud, only be an origin.
- **Secret zero, written down.** The export password, the lab account's
  master password and its 2FA recovery code, the restic password and
  the tofu passphrase: a handful of strings that unlock everything else,
  and none of them can live only in the thing they unlock. Paper, in a
  known place, is the honest answer; Bitwarden's Emergency Access (a
  premium feature) covers the personal account's "what if I can't" case
  for a trusted person.

One honest boundary stays regardless of vendor: a runner reads its env_file
at boot and has no deploy flow, so *its* secret rotation keeps a manual
restart.

### 9. A lab portal — the docs half, then the dashboard half

Two halves with different costs, in that order.

**The docs half: runbooks readable from a phone.** The READMEs are the
runbooks — each slice's `RUNBOOK.md` — and the
drills (#1) are what prove them. What is missing is a way to *follow*
one without a checked-out repo and a text editor. Cheapest stopgap,
available today: Forgejo renders every README in the browser, so
`code.twolfe.dev` on a phone already works. The real thing is a static
docs site built from the repo's markdown — MkDocs Material or similar,
built by a workflow on push on the containerised runner (#6), served by a static container behind caddy at
`docs.twolfe.dev`. One convention decides the rest: procedures
meant to be followed live in a `runbooks/` tree (or a "Runbook" section
per slice README) so the site can put them on a page of their own, apart
from the design prose nobody reads at 2 a.m.

**The dashboard half: a custom build.** One page listing every
service, whether it is up, what it is running, when it was last backed
up and when that backup was last proven. Plug-and-play options exist
(Homepage, Glance, Dashy) and were considered; Tom's preference is to
build it, and the data is already there to build against: Gatus's
read-only API for status, Beszel's for host and container stats, the
Forgejo Actions API for last-run per workflow, restic's snapshot list for backups,
Forgejo's for the repo. A static front end that reads those over the
`lab` network, in a container behind caddy, is a slice like any other.
It does not replace Gatus, Beszel or the Actions tab; it is the page
that saves opening four of them.

## Debt no item above retires

### Jellyfin's state directory is where the native app left it

Jellyfin stays on the mini — the drives are there. This is about the
path, on the same machine: `jellyfin/compose.yaml` bind-mounts
`~/Library/Application Support/jellyfin` at the same absolute path
inside the container and overrides every `JELLYFIN_*_DIR` to match,
because the database stores absolute paths and item IDs derive from
them. Every other slice keeps state under `~/Docker/<slice>`; this is
the one mount rooted in a macOS user directory, and the reason its
restore lands under `Users/tomwolfe/Library/…`. The gain is small —
one convention, one less exception in the restore steps — and the cost
is a one-time move: stop, copy to `~/Docker/jellyfin`, rewrite the path
columns in `jellyfin.db` (or accept a full rescan and lost watch state),
change the four variables. Only worth doing after the restore drills
(#1), and only if the exception bothers you more than the rewrite does.

#2 removes the choice. `~/Library/Application Support` does not exist on
Linux, so the path moves when the slice migrates and the rewrite becomes
part of that step rather than optional cleanup.

## Undecided

### General file sharing (the third thing Google Drive does)

Backup and vault storage have answers above; sharing files with *other
people* doesn't. It's the only item here that would require exposing
something to the internet, which the lab has never done. Options, unranked:
Tailscale node-sharing if "a few known people" covers it; a public
Nextcloud/Seafile if it doesn't, which deserves its own design pass; or
keep a SaaS for the small subset actually shared and self-host the rest.
Explicitly not blocking the backup work — they're independent.

### Pi-hole again — and local DNS for `*.lab`

Today nodes address each other by MagicDNS name
(`<host>.tailf823b8.ts.net`) — no IPs in the repo, but the tailnet suffix
recurs and node-to-node traffic rides the tailnet even on the LAN. Local
names under `*.lab`, with Tailscale split DNS pointing the `lab` domain
at the Pi (in `tailscale/tofu`), are what replaces that.

It ran on the old Pi until it broke (probably SD card wear; this one
boots from NVMe). The
second Pi is the obvious home for it: DNS wants port 53 and real host
networking, which is exactly what macOS cannot give a container.

The argument is not ad-blocking, though. `caddy/README.md` already concedes
the weakness: *"The lab must keep working with the internet down; DNS for
`*.twolfe.dev` lives on Netlify's nameservers and resolves only while
the internet is up."* Local DNS records on a Pi-hole would make lab names
resolve on the LAN with the internet unplugged, closing a gap the repo has
been honest about but has not fixed. It would also retire the router's
DNS-rebind workaround from the other direction.

Cost: DNS becomes a thing that can fail and take the house's internet with
it, so it wants a second resolver configured in DHCP, and it is squarely a
"reduce risk before adding surface" judgement call.

### Router as code

A research project first. Tom likes the idea and
has said he doesn't yet understand it, so this section is the primer
and the questions, not a design.

**What it means.** Today the router's configuration — DHCP
reservations, DNS overrides, firewall rules, port forwards, VLANs if
any — lives in the Archer's web UI and nowhere else. "As code" means
those settings are files in this repo and a tool pushes them to the
device: an OpenTofu provider (a `router/tofu` root, exactly symmetric
with forgejo managing repos and garage managing buckets) or Ansible.
The Archer cannot be driven that way; the routers that can are
OPNsense (open-source firewall OS on a small fanless PC, community
OpenTofu provider, mature Ansible collection) and MikroTik RouterOS
(cheap purpose-built hardware, arguably the best-maintained provider
of the lot, steeper learning curve). No router gets to 100% — the
install, WAN setup and interface assignment stay manual, the same trade
already accepted with Forgejo.

**What it would fix here.** The mini's mDNS flapping between two
interfaces (a DHCP reservation, declared); the DHCP-advertised-DNS
workaround for the Archer's rebind protection (declared, or moot with a
router that has an allowlist); the "second resolver in DHCP" that
Pi-hole wants; and the Wi-Fi/wired split, since the AX55 is both the
router and the flat's only radio and only one of those roles can leave
the utility room — so it is really three purchases: router, access
point, and PoE for it. The Smart Hub stays as the modem regardless;
Digital Voice locks that role, not this one.

**The failure mode to design around.** The router is the one device
where a bad apply removes the ability to undo the bad apply, and takes
the lab's connectivity with it — the Headscale objection, stronger. So
whatever is chosen: a console path that doesn't route through it, a
known-good config export outside it, and never applying from a session
that depends on it. That is an argument for care, not against the idea.

**Two of the cheap fixes don't need any of this.** Turning the mini's
Wi-Fi off and setting a DHCP reservation on the Archer closes the
flapping today, by hand. They are on the rack build's arrival list
and should just be done.

### The container runtime

Tom is not set on Docker, only on containers, and is open to a better
runtime or orchestrator. The changelog is the argument for looking:
`network_mode: host` is a silent no-op (dead mDNS/SSDP discovery, no Home
Assistant on the mini), VirtioFS races on first boot, every container's
port 53 intercepted by the VM, keychain-bound registry pulls in headless
sessions, and an update that took the lab down (0.18.1).

**Nearly all of it is the VM, not Docker.** macOS cannot run Linux
containers; every runtime on the mini — Docker Desktop, OrbStack, Colima,
Podman — boots a Linux VM and shares files and ports across the boundary.
The filesystem races, the port interception and the no-host-network problem
all live at that boundary, and **#2 removes the boundary rather than
changing the runtime**, which answers most of this section: Docker Engine
on Linux, natively, as the Pi already runs it. Trying OrbStack or Colima on
the mini is worth an afternoon only if #2 stays unfunded long enough to
hurt.

**Orchestration is a separate question** and mostly the k8s one below.
Between "compose per host" and Kubernetes there is Docker Swarm (a
multi-host compose, effectively, and still maintained) and not much
else worth the licence terms. With two or three hosts and a runner on
each, compose-per-host with `runs-on` choosing the target is the
smallest thing that works; revisit when
that is what hurts.

### Smart home — Home Assistant on a Raspberry Pi

Wanted (Hue, Sonos, Google/Nest cameras). Undecided only in the sense that
it hasn't been started; the shape is now clear.

First, a reframe: HA is not a monitoring tool, it is a home automation
platform. If the want is only "tell me when a device drops off", **Gatus
already covers most of it** — the Hue bridge and each Sonos speaker
answer on a stable LAN IP, so they are ordinary TCP checks in
`gatus/config/`, costing no new service. Worth doing that first and
seeing what is left.

**Not on the mini.** Docker Desktop on macOS ignores `network_mode: host` —
a documented no-op, the container stays isolated. That kills the mDNS/SSDP
discovery Hue, Sonos and Chromecast rely on; it is the same root cause
already written into `jellyfin/compose.yaml` for the dead discovery port.
On a **Raspberry Pi running Linux, host networking is real**, so the
blocker simply goes away. Use the Pi 5 over the 4: the recorder database is
write-heavy and benefits from the faster I/O.

**It boots from NVMe, not an SD card**, which matters here: HA's recorder
writes constantly, precisely the workload that wears SD cards out.

**HA Container, not HA OS.** HA OS is an appliance: it cannot be a chezmoi
machine, and its config lives inside Supervisor. HA Container on Raspberry
Pi OS makes the Pi an ordinary managed machine and HA an ordinary compose
slice. The cost is losing the add-on ecosystem (Zigbee2MQTT, Node-RED),
which is only a loss if those are wanted.

**On declaring it.** There is no usable OpenTofu provider — the only one
(`Mikescops/homeassistant`) has been unmaintained since January 2021, and
its resources *control* lights and media players rather than declare
configuration, which is Terraform-as-remote-control and the wrong idea
anyway. But that is the wrong axis. HA's native IaC is **YAML in git**:
automations, scripts, scenes, templates and dashboards are all files. Only
integration config entries (`.storage/`, holding OAuth tokens and
discovered devices) are UI-managed — and that is exactly the split this
repo already runs everywhere else, where `caddy/Caddyfile` is code and
the Netlify token is vaulted credential state. The decisions are declarable; only
the credentials aren't. That makes HA *better* on this axis than beszel,
whose alert thresholds have no file representation at all.

**The structural cost is paid.** The Pi is a managed node with its own
runner; what is left here is one compose slice, and the Pi's backup path for
its state.

### New services: Plane, OpenGist

Wanted, but each adds backup surface.

## Hardware worth buying

Not roadmap items, but the physical constraints the items above assume.

**A UPS** — promoted to roadmap item #3; the rack plan places it.

**The Linux primary node** — specced in item #2. The largest planned
expense in the lab, and the one that unblocks storage growth past what a
Thunderbolt enclosure can hold.

**A second backup drive — resolved, no purchase needed.** Backups moved to
`/Volumes/Data2`, which is a separate physical drive with far more room.
What that buys is *decorrelation*: Data1 previously held 1.5 TB of media and
every backup, so one drive failure took both at once. Now one event takes
one thing. Be clear about what it does not buy — the two drives share an
enclosure, so a controller or PSU failure still takes both, as does theft,
fire, or an accidental delete. Restic to B2 remains the actual second
copy; this is a cheap improvement on the way there, not a substitute.
#2 changes the enclosure, not this
property: a cage and a Pico-PSU correlate the same way a DAS and its
controller do. Decorrelation past that point is what the offsite copy is
for.

**Not needed yet:** a Zigbee/Thread coordinator only matters if Home
Assistant grows past the Hue bridge into Matter devices. And the mini's
mDNS flapping between its two interfaces is a config problem (a DHCP
reservation, or disabling the unused interface — see "Router as code"),
not a hardware one.

## Deliberately deferred

### Self-hosted secrets (OpenBao)

Moot twice over: the vault exit (#8) fixes the vendor
question while keeping a cloud origin *by decision* — the server side is
only ever a replica, exactly to avoid the circularity that parked
OpenBao here. The section stays as the record of why.

The original goal was cutting the cloud dependency. The operative part is
already had: the vault is read by deploys and jobs, never by anything
that runs, so a running stack rides out a vault outage and only the next
deploy waits. 1Password Connect left this section
for the config plane (#8), pulled by a different goal —
rotation ergonomics, not cloud-cutting; as a sync cache it dodges the
circularity below. OpenBao stays deferred: it would *own* the secrets,
adding an unseal ritual and a genuine bootstrap circularity — lab down,
can't reach secrets, can't bring lab up. Small remaining gain, real
added fragility. Revisit if the calculus changes.

### Kubernetes + Argo CD (`k8s/`)

Kept as a learning goal, not as a solution to a current problem. The lab
has a working push-based CD loop and a deliberate trust model — a host
runner per node for the lab, a containerised runner for everything else
— and Kubernetes on a single mini via Docker Desktop is a lot of machinery
for one node that would dissolve the vertical-slice model into manifests.
Worth doing if the point is to learn it; worth being honest that it isn't
fixing anything. The trigger: two Linux service nodes whose workloads no
longer want host networking (Pi-hole and Home Assistant do). Flux over
Argo if that day comes — git-native, no UI-owned state.
