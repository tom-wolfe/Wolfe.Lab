# Roadmap

What's coming and *why*, in rough order. `CHANGELOG.md` is the record of
what happened; this is the record of what we decided to do next and what we
decided to leave alone. Items move out of here into the changelog when they
ship.

Ordering principle: reduce risk before adding surface. Anything that makes
a failure visible outranks anything that adds a new thing to fail.

## Next

### 1. Tailscale — a `tailscale/` slice

Host-level client on all three Macs via the Brewfile first; MagicDNS on; no
subnet router. It's early in the list because three things already written
down depend on it:

- the neat public names (`jellyfin.twolfe.dev`) point at a Tailscale IP —
  see the edge/DNS design;
- the portless Forgejo clone URL waits on a dedicated IP with a free port
  22 (`forgejo/compose.yaml` records that decision);
- `100.64/10` isn't touched by the router's DNS-rebind protection, so the
  DHCP-advertised-resolver workaround in `caddy/README.md` can come out.

Per-service sidecars come later, when the Forgejo port-22 case is worth it.
Accepted trade-off: Tailscale's coordination server is a cloud dependency.
Headscale is rejected — if the lab is down you can't reach the lab to fix
it.

**Precondition: get the mini off NordVPN first.** Measured 2026-08-29 — the
mini's default route is already a Nord tunnel (`utun8`, gateway `10.5.0.2`,
NordLynx). Two consequences. Tailscale would still connect, but NAT
traversal through a commercial VPN's shared NAT rarely hole-punches, so
peers fall back to DERP relay: fine for SSH and web UIs, poor for the
`jellyfin.twolfe.dev` streaming case that is one of the reasons for doing
this at all. And *every* outbound connection the lab makes already
traverses Nord — git pulls, brew, lego's ACME calls, Pushover, and the
healthchecks.io ping. With a kill switch that means a Nord outage is
indistinguishable from an internet outage at the alert.

Split tunnelling is not the fix: NordVPN has **no app-level split
tunnelling on macOS** (Apple's Big Sur networking changes; the mini runs
the full direct-download build 10.9.0, so there is no better build to
switch to). Their only macOS option is a browser extension, useless for a
native app.

So the fix inverts it: move the one app that needs a VPN into a container
behind a **`gluetun` sidecar**, and take the host off Nord entirely. One
compose slice, the download client's traffic tunnelled, everything else —
including Tailscale — on the real interface. This is the first half of the
media-automation item below, pulled forward because it is a precondition
here rather than a nice-to-have there.

Order matters: settle this **before** repointing the `*.lab.twolfe.dev`
wildcard at a Tailscale address, or a flaky tunnel takes name resolution
for the whole lab with it. And test it at the desk, not remotely.

### 2. Netlify DNS — import the records that aren't managed yet

`caddy/tofu` declares exactly one record, the `*.lab` wildcard. The rest of
the `twolfe.dev` zone is now hand-made and undeclared: the Proton Mail
migration (2026-08) added MX, SPF, DKIM and DMARC records to move primary
email onto the domain. They work, but they are drift — invisible to a plan,
absent from any review, and unrecoverable from this repo if the zone were
ever lost. Cheap to fix and pure risk reduction, which is why it sits this
high rather than with the feature work.

**Import, do not recreate.** Use `import` blocks and require the plan to
show **zero changes** before applying. Getting an MX or SPF record subtly
wrong doesn't error, it silently stops mail or lands it in spam, and the
feedback loop is days long. `dig MX twolfe.dev` and friends before and
after, and compare.

**It also forces a decision the current model dodges.** `caddy/README.md`
says Netlify is a provider, not a slice, and that per-service records live
in the owning slice's tofu root. Email records own nothing — there is no
mail slice, and Proton is SaaS. Zone-level records (MX, SPF, DMARC, apex,
domain verification) belong to the *domain*, not to any slice. Likeliest
answer is a small `dns/` root owning zone-level records only, leaving the
per-service rule untouched; whether the caddy wildcard moves there too is
the arguable part, since `caddy/README.md` has a reasoned case for keeping
it with the front door it points at.

### 3. Uptime Kuma — a `kuma/` slice

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

### 4. Offsite backup — restic to Backblaze B2

Native restic encryption (repo password from 1Password, same
vault-is-the-origin pattern as everything else). Not only offsite: the
current scheme keeps **ten full tarballs per service**, and restic's
content-addressed dedup plus compression collapses those to roughly one
snapshot plus deltas. It fixes the space problem and the no-second-copy
problem in one move. `garage/rclone.env` already proves the S3 plumbing.

### 5. Jellyfin library cleanup, then the *arr stack

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

### 6. Obsidian vaults into git

Replaces Google Drive as the vaults' storage with an hourly commit-and-push
job to Forgejo. Better on every axis: history, dedup, rides the existing
`lab.forgejo/backup`, and it drops the `~/Library/CloudStorage` dependency
that's the reason sshd needs "Full Disk Access for remote users" granted.

### 7. Renovate as a Kestra flow

Roughly nine pinned images across the slices (kestra, postgres, caddy,
forgejo, jellyfin, garage, lego, beszel). Renovate runs fine as a container
task — it does **not** need the Actions runner — and it automates the "bump
the pin via a normal PR first" ritual `kestra/README.md` currently asks for
by hand.

### 8. Forgejo Actions runner

The big structural unlock, and still the plan of record from phase 3:
CI, the changelog check (Forgejo issue #10), plan-on-PR and apply-on-merge.
It also closes a real correctness gap — Garage has no state locking, so
concurrent `tofu apply`s are currently prevented by operator discipline
alone, and the runner becomes the serialization point.

### 9. Local models on the Mac Studio (hardware lands ~late Sept 2026)

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

### Self-hosted secrets (1Password Connect / OpenBao)

The original goal was cutting the cloud dependency. The `create_` template
pattern already achieves the operative part: `op` is a bootstrap
dependency, not a tick dependency, and rotation is delete-the-file-and-
apply. OpenBao would add an unseal ritual and a genuine bootstrap
circularity — lab down, can't reach secrets, can't bring lab up. Small
remaining gain, real added fragility. Revisit if the calculus changes.

### Kubernetes + Argo CD (`k8s/`)

Kept as a learning goal, not as a solution to a current problem. The lab
has a working pull-based CD loop and a clean security model — no
*container* gets the Docker socket, one audited SSH door — and Kubernetes on
a single mini via Docker Desktop is a lot of machinery for one node that
would dissolve the vertical-slice model into manifests. Worth doing if the
point is to learn it; worth being honest that it isn't fixing anything.
