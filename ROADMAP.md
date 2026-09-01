# Roadmap

What's coming and *why*, in rough order. `CHANGELOG.md` is the record of
what happened; this is the record of what we decided to do next and what we
decided to leave alone. Items move out of here into the changelog when they
ship.

Ordering principle: reduce risk before adding surface. Anything that makes
a failure visible outranks anything that adds a new thing to fail.

## Next

### 1. Uptime Kuma — a `kuma/` slice

Endpoint checks, which is the one shape of failure nothing here currently
sees. Beszel is agent-based: it reports that a host is loaded or a container
has stopped. It cannot tell you a container is *up and wedged* — and that
gap is not hypothetical, it is exactly how caddy's healthcheck sat red for
33 hours while caddy served traffic perfectly. A request is the only thing
that finds that class of fault.

Two customers, and they are different jobs:

- **off-stack projects** — the actual ask, and nothing in the lab does it
  today. Outbound HTTP checks; the lab still exposes nothing inbound.
- **lab services** — `jellyfin.lab.twolfe.dev` and friends, answering
  through the front door rather than merely running.

**Run it on the Pi, not the mini.** Once a second node exists (see the smart
home item), Kuma there is genuinely external to the *mini* — so it catches
"the mini is down", which today only healthchecks.io sees, and only after a
ten-minute grace. On the mini it cannot see the one failure you most want.

**Decided 2026-08-31: wait for the Pi rather than land on the mini as a
stopgap.** No Pi exists as a managed node yet (nothing on the tailnet, rack
kit still arriving), so this item queues behind the Pi joining the fleet —
it does not jump the queue by compromising on placement.

**What it is still NOT: an outside observer.** Kuma anywhere in the house
shares the house's fate — power cut, router dead, silence that looks like
health. That is the whole
reason `chezmoi/tofu/` puts the tick's dead man's switch on healthchecks.io,
and Kuma does not replace it. Nor does it replace `lab.beszel/health`:
something has to watch the watcher, and a watcher that watches itself isn't
one.

Costs, named now rather than discovered later: it would be the **second**
slice whose configuration is UI-only (no OpenTofu provider), after beszel —
a pattern worth watching if a third ever appears. Its SQLite state adds a
backup flow. Small, though — nowhere near the Immich-class surface below.

Placed here because it is cheap and the need is real, but note the tension
with the ordering principle at the top: offsite backup below is pure risk
reduction against the lab's biggest remaining single point of failure (one
copy, one drive, one machine), whereas this both adds visibility *and* adds
a thing to fail. Swapping the two would be defensible.

### 2. Offsite backup — restic to Backblaze B2

**Built 2026-09-01 (see `restic/README.md`); remaining work is the
bootstrap** — B2 account, vault items, `restic init`, first seed, restore
drill.

Redesigned on the way in (Tom's call): an earlier version of this entry
proposed restic *on top of* the tarball scheme, deduplicating the ten
`.tar.gz` per service. That was backups chained on backups — restic now
**replaces** tar inside the same stop windows, and the keep-10 pruning
gave way to one retention policy in `lab.restic/offsite`. Local repo on
Data2 for fast restores and internet-down nights, `restic copy` to B2 as
idempotent catch-up, weekly `restic check` against both repos, and a
dead man's switch on the offsite run. Google Drive content stays out of
scope until it's been sorted through; Immich still waits for this to be
proven (a restore drill), not just merged.

### 3. Jellyfin library cleanup, then the *arr stack

Two phases, and the first is the valuable one.

**Phase A — Sonarr and Radarr as a renamer.** Pointed at the existing
library with no indexers and no download client, they do exactly the job
that was otherwise going to be a bespoke script: parse what's there, and
rename and re-file it into a consistent layout. Two containers, no VPN
dependency, nothing else in this item required. It can be pulled forward
whenever — it sits here only because the rest of the item does.

The usual objection doesn't apply: renaming changes every path, every path
change mints a new item ID, and watch history hangs off item IDs
(`jellyfin/README.md`). Normally that's the thing that stops you. Watch
stats are explicitly not wanted here, so the cost is a library rebuild and
nothing else. Take a `lab.jellyfin/backup` first anyway — it's nightly and
free — and do it in one pass rather than trickling, so there's one scan and
one rebuild.

**Phase B — acquisition.** Prowlarr for indexers, a download client, and
Jellyseerr as the "write it down and forget it" front end: request
something, it lands on a list, and it arrives without further involvement.
Jellyseerr reads the Jellyfin library, so it won't offer what's already
there.

Note the download client and its `gluetun` sidecar have already moved up
into the Tailscale item — they're a precondition there, not a bonus here.

**The cost, plainly:** Phase B is four or five more containers to pin,
upgrade, back up and monitor. That is the "each adds backup surface" line
below, and this is the first item to really test it. Mitigating: the
configs are small SQLite databases, nothing like the Immich case. The media
itself is already unprotected either way — 1.6 TB of it, against 3.3 GB of
backups — which is an argument for the offsite item above landing first.

### 4. Obsidian vaults into git

Replaces Google Drive as the vaults' storage with an hourly commit-and-push
job to Forgejo. Better on every axis: history, dedup, rides the existing
`lab.forgejo/backup`, and it drops the `~/Library/CloudStorage` dependency
that's the reason sshd needs "Full Disk Access for remote users" granted.

### 5. Renovate as a Kestra flow

Roughly nine pinned images across the slices (kestra, postgres, caddy,
forgejo, jellyfin, garage, lego, beszel). Renovate runs fine as a container
task — it does **not** need the Actions runner — and it automates the "bump
the pin via a normal PR first" ritual `kestra/README.md` currently asks for
by hand.

### 6. Forgejo Actions runner — CI only

Rescoped 2026-08-31: CD went to Kestra (plan-on-tick, apply-on-tap,
kestra's root auto — kestra/README.md "OpenTofu CD"), applies are
serialized by being Kestra flows, and the runner **never applies** — the
old "two apply sources" hazard is resolved by decree rather than
sequencing. What remains is review quality: lint (#4, #6), plan-on-PR
(#8) and the changelog check (#10). Two flags for that design pass:
plan-on-PR needs provider credentials, and handing op secrets to a
workflow that pre-merge code can edit is the classic
`pull_request_target` foot-gun — theoretical while every PR is Tom's
own, but it should be named in the workflow design. And the runner must
not hold the Docker socket: that trust was extended to Kestra, which
runs only merged code — never to the component that touches PRs.

### 7. Local models on the Mac Studio (hardware lands ~late Sept 2026)

Pre-ordered M5 Ultra, ~4 weeks out. **Decided: it is a second node, not the
mini's replacement — and it is a workstation, not a server.** WiFi, powered
off when unused, and deliberately kept free of ambient load. That single
fact settles most of the design.

**It is shaped like the laptops, not like the mini.** A chezmoi machine
that Tailscale can reach; *not* a `lab-job` bridge target and not a home for
any `lab.<slice>/deploy` flow, because both assume always-on. Correcting
something written here earlier: the Studio does **not** put a deadline on
the multi-host fleet work — that pressure comes entirely from the Pi, which
is the machine that will actually run services. The Studio needs Tailscale
and nothing else structural.

**"Not always on" costs nothing here, because the jobs are interactive.**
Querying the vault and dictating notes are things done *sitting at the
machine*. There is no unattended workload to strand.

The three stated jobs are three different tools:

- **Vault querying** — Ollama plus a RAG front end. Rides the Obsidian
  vaults-into-git item, which conveniently turns the corpus into a git
  checkout instead of a CloudStorage mount.
- **Capturing notes** — Whisper-family transcription, local.
- **Filing paperwork** — Paperless-ngx, and **this one belongs on the
  mini**, not the Studio. It is an always-on ingest-and-index service, its
  OCR is CPU-bound, and it wants to accept documents whether or not the
  desktop is awake. Only LLM-assisted tagging would reach for the Studio,
  and that can degrade to "tag it later" when the machine is off. It also
  needs no new hardware, so it could be built today.

**Ollama must be a HOST process, not a container** — a container on macOS
gets no access to the Apple GPU, so a containerised model runs on CPU and is
uselessly slow. `ollama` is already in the Brewfile's personal section, so
this is a widening rather than a new dependency. Run it **on demand rather
than via `brew services`**, per the no-ambient-load requirement; an idle
Ollama is cheap (it unloads models after a keep-alive) but "cheap" is not
"nothing" on a machine being used for other work.

**Beszel: add it with Status alerts OFF**, exactly like the laptops. A
workstation that is off is not a failure, and a Status alert would page on
every shutdown.

### 8. A config plane — Garage for configuration, the 1Password exit for secrets

Designed 2026-08-31 (the poke's webhook URL forced the config half; the
rotation dance pulled the secrets half up out of "Deliberately
deferred" the same day). Two halves because they are the same move —
resolution goes LAN-local while every reference keeps its shape — and
they are separable builds, each landing when its own pressure arrives:
the Garage half on the SECOND cross-slice value, the Connect half the
next time secret rotation actually bites.

**The config half.** Today `forgejo/tofu` constructs the tick's webhook
URL from the tick's flow file and kestra's compose file — the
monorepo-as-stack-reference move. It works, and it fails loudly — but it
is still a *consumer's guess* at a URL whose format belongs to the
kestra slice, port 8080 and all.

The pattern, when that second value appears: a dedicated `config` bucket
in Garage. The OWNING slice's tofu root publishes named values as S3
objects — kestra/tofu would derive `tick_webhook_url` from the files it
already reads, and it auto-applies on the tick, so the publisher is
never more than fifteen minutes stale. Consumers list-then-read (a list
tolerates absence where a read errors) and `count` the dependent
resource on presence, so a root deploying before its dependency simply
omits the wiring and picks it up on a later plan: partial deployment
breaks bootstrap cycles, and the plan-on-tick nag makes the eventual
consistency visible instead of silent. Two caveats, named at design
time. The failure mode INVERTS — absent config is a green plan that
deploys nothing, so every consumer carries a tofu `check` making absence
at least a named warning (the `chezmoi/tofu` pattern). And published
config is declared truth, not liveness — proving someone answers is
monitoring's job (the Kuma item), not this one's.

Why Garage and not the vault: configuration and secrets are different
jobs. 1Password is the origin of *secrets*, is a cloud round-trip — the
whole reason `create_` templates evaluate once instead of pinging it
every tick — and changing a value there means the delete-and-recreate
dance. Garage is LAN-local (every plan already polls it), sits below
every would-be publisher in the bootstrap order (using Kestra's own KV
store would recreate the very circularity this breaks), and costs no new
service.

**The secrets half — rewritten 2026-09-01: leave 1Password for the
Bitwarden ecosystem.** Tom's decision, on ethical grounds, with the
rate-limit outage as the forcing function; the 1Password Connect design
that stood here is superseded (it solved the rate limit but kept the
vendor). Constraints fixed at decision time, so the design pass starts
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
  quota and survive cloud outages. A small run-style shim keeps the
  committed env-files-of-references pattern, and chezmoi has native
  `bitwarden`/`rbw` template functions for the `create_` templates.
- **Migration surface, inventoried:** every vault item, every
  `secrets.env`, the `create_` templates, the op service account (secret
  zero changes shape), the SSH agent on three Macs, the READMEs.
- **Sequenced after restic ships:** the offsite copy exists first, then
  the origin moves.

One honest boundary stays regardless of vendor: kestra reads `SECRET_*`
at boot and has no deploy flow, so *its* secret rotation keeps a manual
restart.

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

It ran on the old Pi until it broke (probably SD card wear, see below). The
second Pi is the obvious home for it: DNS wants port 53 and real host
networking, which is exactly what macOS cannot give a container.

The argument is not ad-blocking, though. `caddy/README.md` already concedes
the weakness: *"The lab must keep working with the internet down; DNS for
`*.lab.twolfe.dev` lives on Netlify's nameservers and resolves only while
the internet is up."* Local DNS records on a Pi-hole would make lab names
resolve on the LAN with the internet unplugged, closing a gap the repo has
been honest about but has not fixed. It would also retire the router's
DNS-rebind workaround from the other direction.

Cost: DNS becomes a thing that can fail and take the house's internet with
it, so it wants a second resolver configured in DHCP, and it is squarely a
"reduce risk before adding surface" judgement call.

### Smart home — Home Assistant on a Raspberry Pi

Wanted (Hue, Sonos, Google/Nest cameras). Undecided only in the sense that
it hasn't been started; the shape is now clear.

First, a reframe: HA is not a monitoring tool, it is a home automation
platform. If the want is only "tell me when a device drops off", **Uptime
Kuma above already covers most of it** — the Hue bridge and each Sonos
speaker answer on a stable LAN IP, so they are ordinary TCP checks costing
no new service. Worth doing that first and seeing what is left.

**Not on the mini.** Docker Desktop on macOS ignores `network_mode: host` —
a documented no-op, the container stays isolated. That kills the mDNS/SSDP
discovery Hue, Sonos and Chromecast rely on; it is the same root cause
already written into `jellyfin/compose.yaml` for the dead discovery port.
On a **Raspberry Pi running Linux, host networking is real**, so the
blocker simply goes away. Use the Pi 5 over the 4: the recorder database is
write-heavy and benefits from the faster I/O.

**Boot it from USB/NVMe, not an SD card.** HA's recorder writes constantly,
which is precisely the workload that wears SD cards out — the likeliest
explanation for why the Pi-hole on the old Pi "broke".

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
`caddy.env` is vaulted credential state. The decisions are declarable; only
the credentials aren't. That makes HA *better* on this axis than beszel,
whose alert thresholds have no file representation at all.

**The real cost is structural, and it is bigger than HA.** Every flow's SSH
task targets `host.docker.internal` — the model assumes exactly one managed
host. A Pi means a second `lab-job` bridge with its own key and its own
repo checkout, flows that choose a target, and a `chezmoi/home/` tree that
currently assumes macOS throughout (Brewfile, `defaults`, LaunchAgents).
Tailscale above makes the addressing sane, and none of it is hard — but it
is a genuine expansion of the fleet model, and it should be costed as that
rather than as "add a container".

### New services: Plane, OpenGist, Immich

Wanted, but each adds backup surface. Immich in particular is large and is
the one where data loss actually hurts — it should land *after* offsite
backup exists, not before.

## Hardware worth buying

Not roadmap items, but the physical constraints the items above assume.

**A UPS — the strongest recommendation here.** Everything runs on one mini
with USB-attached drives, and the nightly backups are `stop → tar → start`
against LMDB and SQLite. A power blip mid-write is how those get corrupted,
and the corruption is silent until a restore fails. macOS handles a UPS
natively for shutdown, and `apcupsd`/NUT makes it monitorable, so it lands
in beszel or Home Assistant like anything else.

**A second backup drive — resolved, no purchase needed.** Backups moved to
`/Volumes/Data2`, which is a separate physical drive with far more room.
What that buys is *decorrelation*: Data1 previously held 1.5 TB of media and
every backup, so one drive failure took both at once. Now one event takes
one thing. Be clear about what it does not buy — the two drives share an
enclosure, so a controller or PSU failure still takes both, as does theft,
fire, or an accidental delete. Restic to B2 above remains the actual second
copy; this is a cheap improvement on the way there, not a substitute.

**NVMe or USB SSD for the Pi**, per the smart home item — HA's recorder is
write-heavy and SD cards are what fail.

**Not needed yet:** a Zigbee/Thread coordinator only matters if Home
Assistant grows past the Hue bridge into Matter devices. And the mini's
mDNS flapping between its two interfaces is a config problem (a DHCP
reservation, or disabling the unused interface), not a hardware one.

## Deliberately deferred

### Self-hosted secrets (OpenBao)

Doubly moot since 2026-09-01: the vault exit (item 8) fixes the vendor
question while keeping a cloud origin *by decision* — the server side is
only ever a replica, exactly to avoid the circularity that parked
OpenBao here. The section stays as the record of why.

The original goal was cutting the cloud dependency. The `create_` template
pattern already achieves the operative part: `op` is a bootstrap
dependency, not a tick dependency. 1Password Connect left this section
2026-08-31 for the config plane (item 8), pulled by a different goal —
rotation ergonomics, not cloud-cutting; as a sync cache it dodges the
circularity below. OpenBao stays deferred: it would *own* the secrets,
adding an unseal ritual and a genuine bootstrap circularity — lab down,
can't reach secrets, can't bring lab up. Small remaining gain, real
added fragility. Revisit if the calculus changes.

### Kubernetes + Argo CD (`k8s/`)

Kept as a learning goal, not as a solution to a current problem. The lab
has a working pull-based CD loop and a deliberate trust model — Kestra
holds the socket as the platform (decision 2026-08-31, kestra/README.md),
one SSH transport for host work — and Kubernetes on
a single mini via Docker Desktop is a lot of machinery for one node that
would dissolve the vertical-slice model into manifests. Worth doing if the
point is to learn it; worth being honest that it isn't fixing anything.
