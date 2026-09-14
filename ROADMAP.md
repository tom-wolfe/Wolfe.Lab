# Roadmap

What's coming and *why*, in rough order. `CHANGELOG.md` is the record of
what happened; this is the record of what we decided to do next and what we
decided to leave alone. Items move out of here into the changelog when they
ship.

Ordering principle: reduce risk before adding surface. Anything that makes
a failure visible outranks anything that adds a new thing to fail.

## Next

### 1. Restore drills — prove the backups before anything depends on them

`restic/` shipped in 0.16 and nothing has been restored from it. `restic/README.md` asks for a drill after bootstrap and gates
deleting the old tarball farm on it; the vault exit (#10) was sequenced
"after restic ships". A backup nobody has restored from has not shipped,
so this is the first item by the ordering principle: it makes the one
failure that is silent until it is total — an unrestorable backup —
visible on a schedule.

**It is several drills, not one, and that is fine.** Every service is
snapshotted separately (one `backup.conf` per slice), so there is one
restore per service. But the shape is
the same shape as the backups: ONE workflow that walks
the same `backup.conf` files the backup pipeline reads, restores each
service's latest snapshot to a scratch path, and asserts that the thing
the service needs to boot is there and non-empty — forgejo's `gitea.db`,
jellyfin's `jellyfin.db`, sonarr's `sonarr.db`, radarr's `radarr.db`,
beszel's `data.db`. Weekly, after `verify`, alerting on failure. Honest scope: this proves the
files come back, not that the service boots on them.

**The boot-on-restore drill is manual and occasional.** Once a quarter:
pick a service, stop it, move its state aside, restore, start it on the
tagged image, confirm it works, put the original back. This is the one
that catches the "restored but the app doesn't like it" class, and it
is what turns each slice's `RUNBOOK.md` restore steps into something someone has
actually followed. Those sections are where the steps live; the docs
site (#11) is what makes them followable from a phone.

**A cold-start runbook belongs here too.** Mini and Data2 both gone:
what is needed from outside the building (the restic password, the tofu
state passphrase, the B2 credentials, the vault's own master password —
see the secret-zero note in #10), and in what order things come up
(garage before any tofu root, caddy before any route, Forgejo before any
workflow). Nothing in the repo says this today; `setup.sh` is the closest.

### 2. The Pi as a node — what's left

The Pi boots from NVMe, is a chezmoi machine, runs both Actions runners
and the Beszel agent, and hosts Gatus. Two structural pieces remain
before more state lands on it, and both should be decided before the
first stateful slice does, not after:

- **Backups.** Whatever state lands on the Pi gets a `backup.conf` and
  a restic path like anything else; the Pi needs restic, the repo
  password, and a route to `/Volumes/Data2/restic` or its own local
  repo that the offsite copy also ships. Gatus has no state worth
  keeping, which is why this could wait until now.
- **Tailscale, and the first `tailscale/tofu` root.** Two terms, since
  they come up again in #9 and #10. A *tofu root* is just a directory
  with its own OpenTofu state — every slice's `tofu/` is one; `plan`
  and `apply` run per root. The *Tailscale ACL* is the tailnet's
  access policy: a JSON document saying which devices (or tags) may
  reach which devices on which ports. The default is "everything can
  reach everything", which is what the lab runs today, and it is fine
  for three Macs. `tailscale/README.md` deferred a root "until the ACL
  carries intent". Two Pis, a Studio and a sidecar-per-service pattern
  are that intent: tags per role (`tag:server`, `tag:workstation`),
  the workstation allowed to reach services but nothing allowed to
  reach the workstation, key expiry disabled only on tagged servers.
  Declared in a root, applied like any other. The same root is where
  split DNS for `*.lab` lands (Pi-hole, under Undecided).

Keep the SD card in a drawer, labelled: it is the console of last resort
for a machine with no display.

### 3. The platform layer moves to Linux

Every container fault in the changelog is the VM boundary on macOS, and
no runtime crosses it (OrbStack's host networking does not carry mDNS to
the Wi-Fi interface). The mini keeps what genuinely needs it — the
drives and everything that reads them, the CloudStorage syncs until the
vaults move into git (#6), Homebrew. Nothing in the platform layer does:
Forgejo, its Postgres-free SQLite, Caddy, Garage and the Beszel hub all
publish arm64 images. Move them to the Pi one slice at a time, Forgejo
last (it runs the deploys), and see how much pain is left on the mini.
If the answer is "most of it", the always-on server becomes a Linux box
— a hardware decision (an N100-class machine fits the rack) — and the
mini becomes the media and Apple-specific host. Depends on #2's backup
path, since every one of these has state.

### 4. A UPS

Not bought yet. The rack plan already places it: a ~650 VA unit on the floor beside the
rack, one battery-backed outlet feeding a plain (non-surge) strip on the
rear rail, so the router and switch ride out a blip along with the mini.
The case is unchanged: everything runs on one mini with USB-attached
drives, and the nightly backups stop and start SQLite and LMDB stores.
A power cut mid-write is how those corrupt, silently, until a restore
fails — which is why this sits directly under the drills.

**Buying it is the smaller half.** The item is what happens on battery:

- **USB to the mini, and macOS does the shutdown natively** — System
  Settings → Energy → UPS: shut down after N minutes on battery. Set it
  short; the point is a clean stop, not runtime. `pmset -g batt` shows
  what the OS sees.
- **"Start up automatically after a power failure"** in the same pane,
  so the return of mains brings the mini back without a hand. Docker
  Desktop starts on login, the media stacks already carry mount guards
  for the drive race — verify the whole chain once by pulling the plug.
  That is a drill, and it belongs on the same quarterly rota as the
  restore drill.
- **Making it visible.** Beszel does not read UPS state. NUT (`nut` in
  Homebrew) on the mini exposes it — battery charge, load, on-battery
  events — and both Home Assistant and Gatus can consume that later.
  Not required for the shutdown to work; required for "the battery is
  five years old and holds nothing" to be noticed before the day it
  matters.

### 5. The *arr stack, Phase B — acquisition

Phase A shipped in 0.19.0 (`sonarr/`, `radarr/` as renamers, alongside
the Jellyfin 12.0 upgrade), so what remains is the half that was always
the smaller value and the larger cost.

**Phase B — acquisition.** Prowlarr for indexers, and Jellyseerr as the
"write it down and forget it" front end: request something, it lands on
a list, and it arrives without further involvement. Jellyseerr reads the
Jellyfin library, so it won't offer what's already there. The download
client and its `gluetun` sidecar shipped as `qbittorrent/` (0.12); the
renamers shipped as Phase A — so Phase B is two new containers plus
wiring: Prowlarr → Sonarr/Radarr (API keys, which today sit unused in
each app's `config.xml`), Sonarr/Radarr → qBittorrent (categories with
per-category save paths on `/Volumes/Data2`), and Connect → Jellyfin so
each import triggers a library update instead of the manual scans Phase
A needed.

**The cost, plainly:** two or three more containers to pin, upgrade,
back up and monitor — the "each adds backup surface" line below, and
this item is the first to really test it. Mitigating: the configs are
small SQLite databases, nothing like the Immich case. The media itself
is not backed up at all — 1.6 TB, against single-digit GB of service
state in restic — and whether it should be is an open question
("Backing up media" under Undecided). Phase B changes that question's
shape: a library that re-acquires itself is a library whose loss is an
inconvenience.

### 6. Obsidian vaults into git

Replaces Google Drive as the vaults' storage with an hourly commit-and-push
job to Forgejo. Better on every axis: history, dedup, rides the existing
backup workflow, and it drops the `~/Library/CloudStorage` dependency
that's the reason the Actions runner needs Full Disk Access granted.

### 7. Knowing what's stale — Renovate, and a report for everything else

**Renovate as a workflow.** A dozen pinned images across the slices
(caddy, forgejo, jellyfin, garage, lego, beszel, gatus, gluetun,
qbittorrent, sonarr, radarr, tailscale). Renovate runs fine as a container task — it does **not** need
the Actions runner — and it automates the "bump the pin via a normal PR
first" ritual the slice READMEs ask for by hand.

**The report.** Renovate reads pins in the repo. It
does not read a Brewfile, and it knows nothing about macOS or Docker
Desktop — and since 0.18.2 removed the nightly upgrade flow (unattended
upgrades on a lone server were unwise, and kept failing on prompts),
nothing bumps those and nothing says they are behind. That is the gap:
not the upgrading, which is rightly manual now, but the *knowing*. One
weekly workflow that runs `brew outdated` and
`softwareupdate -l` on the mini and reports — never applies. Same
principle as the free-space check: one poller that reports, not N guards
that act.

**Where the report goes.** Pushover is fine for "3 packages outdated";
it is a poor fit for a list (1024-character messages, no formatting).
This is the first thing in the lab that is a *report* rather than an
*alert*, and reports want email. Nothing in the lab can send one today.
Options, with the honest costs:

- **Proton, directly: not on a personal plan.** Proton offers SMTP
  submission only on business plans. Proton Bridge exposes a local
  SMTP port and has a headless mode, but it is a logged-in session
  that has to live somewhere — the same shape as `op` on the mini, and
  it went about as well. Possible, not recommended.
- **A transactional provider, sending as `lab@twolfe.dev`** —
  recommended. Resend, Postmark, Mailgun, SES; the free tiers cover a
  lab's volume many times over. Sending from the domain needs the
  provider's DKIM record and its `include:` added to the SPF record —
  both in `dns/tofu`, which already holds Proton's; SPF is ONE record
  per domain, so the two includes share it. Receiving is untouched:
  Proton still owns MX, so mail *to* the domain lands where it does
  now, and a reply to a lab report arrives in the inbox. Gatus has a
  native email alert and a workflow step can submit over SMTP, so both
  consume the same credential from the vault. Cost: one more third-party
  dependency, one more `dependencies` check in Gatus.

### 8. CI, the build pool, and pipelines as a CLI

Every node runs a **host** runner for the lab's own CD: repo-scoped, host
mode, holding the node's vault token. The Pi also runs a **containerised**
runner: instance-scoped, the role label `docker`, no host environment and
no socket in a job, where CI runs today (`ci.yaml`: shellcheck and YAML
parsing on every pull request) and where Ritten and NSchema will build and
publish once their repos flip from mirror to active. Three things remain:

- **The mini joins the build pool.** The containerised runner as a compose
  slice, identical on every node — the runner image, socket-mounted so it
  can start sibling job containers, config and registration rendered by
  chezmoi as now — deployed by each node's host runner. On the Pi that
  replaces the systemd-supervised second runner. One mechanical detail:
  a runner in a container hands job containers bind mounts by host path,
  so its work directory must be the same path inside and out; Forgejo's
  own compose example does exactly that.
- **Plan-on-PR and the changelog check.** Plan-on-PR needs provider
  credentials in a pre-merge context, and handing secrets to a workflow
  that pre-merge code can edit is the classic `pull_request_target`
  foot-gun — theoretical while every PR is Tom's own, but it decides the
  design. A `pull_request` workflow can also name a host runner's label
  and run unmerged code on a node; branch protection on main and CI on
  the containerised runner only are the mitigations, and a host runner
  is assumed reachable by any workflow file on any branch.
- **Pipelines as a CLI.** The workflows are deliberately thin — a
  trigger and one command — because the commands are moving into a CLI
  built on Tom's .NET library Ritten. The YAML is the trigger, not the
  pipeline; when the CLI lands, each workflow changes one line.

### 9. Local models on the Mac Studio (hardware lands ~late Sept 2026)

Pre-ordered M5 Ultra, ~4 weeks out. **Decided: it is a second node, not the
mini's replacement — and it is a workstation, not a server.** WiFi, powered
off when unused, and deliberately kept free of ambient load. That single
fact settles most of the design.

**It is shaped like the laptops, not like the mini.** A chezmoi machine
that Tailscale can reach; *not* a deploy target and not a runner host,
because both assume always-on. Correcting
something written here earlier: the Studio does **not** put a deadline on
the multi-host fleet work — that pressure comes entirely from the Pi (#2),
which is the machine that will actually run services. The Studio needs
Tailscale and nothing else structural — though it is the second machine
after the Pi that gives the Tailscale ACL (#2) a reason to say
something: a workstation that reaches services and is reached by nothing.

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
  and that can degrade to "tag it later" when the machine is off. It
  needs no new hardware, so it is listed with the other new services
  below rather than waiting here.

**Ollama must be a HOST process, not a container** — a container on macOS
gets no access to the Apple GPU, so a containerised model runs on CPU and is
uselessly slow. `ollama` is already in the Brewfile's personal section, so
this is a widening rather than a new dependency. Run it **on demand rather
than via `brew services`**, per the no-ambient-load requirement; an idle
Ollama is cheap (it unloads models after a keep-alive) but "cheap" is not
"nothing" on a machine being used for other work.

### 10. A config plane — Garage for configuration, the Bitwarden exit for secrets

Two halves because they are the same move —
resolution goes LAN-local while every reference keeps its shape — and
they are separable builds, each landing when its own pressure arrives:
the Garage half on the SECOND cross-slice value, the secrets half once
the restore drills (#1) have passed — the offsite copy exists first and
is proven, then the origin moves.

**The config half.** No cross-slice value has needed a home yet: the
nearest candidates are the nodes' addresses (today MagicDNS names typed
into the slices that need them) and the ports the Gatus checks carry.
Hardcoding one slice's fact into another is the thing to refuse; a
consumer deriving it from the owning slice's files is only a consumer's
guess at a format that isn't its own.

The pattern, when the first value appears: a dedicated `config` bucket
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
at least a named warning (the `chezmoi/tofu` pattern). And published
config is declared truth, not liveness — proving someone answers is
monitoring's job (`gatus/`), not this one's.

Why Garage and not the vault: configuration and secrets are different
jobs. 1Password is the origin of *secrets*, is a cloud round-trip — the
whole reason `create_` templates evaluate once instead of pinging it
every tick — and changing a value there means the delete-and-recreate
dance. Garage is LAN-local (every plan already polls it), sits below
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
  quota and survive cloud outages. A small run-style shim keeps the
  committed env-files-of-references pattern, and chezmoi has native
  `bitwarden`/`rbw` template functions for the `create_` templates.
- **Migration surface, inventoried:** every vault item, every
  `secrets.env`, the `create_` templates, the op service account (secret
  zero changes shape), the SSH agent on three Macs, the READMEs.

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

### 11. A lab portal — the docs half, then the dashboard half

Two halves with different costs, in that order.

**The docs half: runbooks readable from a phone.** The READMEs are the
runbooks — each slice's `RUNBOOK.md` — and the
drills (#1) are what prove them. What is missing is a way to *follow*
one without a checked-out repo and a text editor. Cheapest stopgap,
available today: Forgejo renders every README in the browser, so
`code.twolfe.dev` on a phone already works. The real thing is a static
docs site built from the repo's markdown — MkDocs Material or similar,
built by a workflow on push on the containerised runner (#8), served by a static container behind caddy at
`docs.lab.twolfe.dev`. One convention decides the rest: procedures
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

## Undecided

### General file sharing (the third thing Google Drive does)

Backup and vault storage have answers above; sharing files with *other
people* doesn't. It's the only item here that would require exposing
something to the internet, which the lab has never done. Options, unranked:
Tailscale node-sharing if "a few known people" covers it; a public
Nextcloud/Seafile if it doesn't, which deserves its own design pass; or
keep a SaaS for the small subset actually shared and self-host the rest.
Explicitly not blocking the backup work — they're independent.

### Backing up media

`restic/` deliberately scopes the media out: service
state is single-digit GB and costs pennies; the library is 1.6 TB. It
sits on Data1 with no second copy anywhere, and no decision has been
recorded either way. The options, so the decision can be made rather
than deferred:

- **Nothing, on purpose.** Media is re-acquirable, and Phase B of #5
  makes re-acquiring it a list rather than a project. The cost is time
  and bandwidth on the day, not money every month.
- **Some of it.** A `keep/` subtree for the things that actually can't
  be found again — one more path in a `backup.conf`, and B2 charges by
  the byte. Requires sorting, which is the thing Drive's content also
  waits on.
- **All of it.** B2 is ~$6/TB/month, so roughly $10/month for the
  library as it stands and growing with it; restic dedups and
  compresses nothing on media, so the bill is the raw size. Restore
  egress is free up to a multiple of what's stored.

Tom's position: some of it may be worth keeping, and the
cost is the thing to balance. The middle option is the likely shape;
the sorting is the work.

### Pi-hole again — and local DNS for `*.lab`

Today nodes address each other by MagicDNS name
(`<host>.tailf823b8.ts.net`) — no IPs in the repo, but the tailnet suffix
recurs and node-to-node traffic rides the tailnet even on the LAN. Local
names under `*.lab`, with Tailscale split DNS pointing the `lab` domain
at the Pi (#2's root), are what replaces that.

It ran on the old Pi until it broke (probably SD card wear, see #2). The
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

Tom is not set on Docker, only on containers, and is
open to a better runtime or orchestrator. The changelog is the argument
for looking: `network_mode: host` is a silent no-op (dead mDNS/SSDP
discovery, no Home Assistant on the mini), VirtioFS races on first
boot, every container's port 53 intercepted by the VM, keychain-bound
registry pulls in headless sessions, and an update that took the lab
down (0.18.1). Worth being precise about where those come from, because
it decides what a switch could fix.

**Nearly all of it is the VM, not Docker.** macOS cannot run Linux
containers; every runtime on the mini — Docker Desktop, OrbStack,
Colima, Podman — boots a Linux VM and shares files and ports across the
boundary. The filesystem races, the port interception and the no-host-
network problem live at that boundary. OrbStack is the one worth an
afternoon: faster, lighter, free for personal use, and it advertises
host networking — verify that mDNS discovery actually works through it
before believing it. Colima is the open-source equivalent without that
claim. Neither changes the compose files.

**The Pi is the first non-VM runtime the lab will have** (#2): Docker
Engine on Linux, natively, where host networking is real and the VM
problems simply don't exist. The honest experiment is to move the
workloads that suffer from the VM there and see how much pain is left
on the mini before switching anything on it. If the answer is "most of
it", the bigger question is whether the always-on server should be a
Linux box at all — which is a hardware decision (an N100-class machine
sits in the same rack unit as the router candidates above), not a
runtime one, and the mini becomes the media and Apple-specific host.

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
`caddy.env` is vaulted credential state. The decisions are declarable; only
the credentials aren't. That makes HA *better* on this axis than beszel,
whose alert thresholds have no file representation at all.

**The structural cost is paid.** The Pi is a managed node with its own
runner; what is left here is one compose slice, and #2's backup path for
its state.

### New services: Plane, OpenGist, Immich, Paperless-ngx

Wanted, but each adds backup surface. Immich in particular is large and is
the one where data loss actually hurts — it should land *after* the
restore drills (#1) have passed, not before. Paperless-ngx (the
paperwork half of #9) belongs on the mini, needs no new hardware, and
could be built today; it is listed here so it isn't lost inside the
Studio item.

## Hardware worth buying

Not roadmap items, but the physical constraints the items above assume.

**A UPS** — promoted to roadmap item #4; the rack plan places it.

**A second backup drive — resolved, no purchase needed.** Backups moved to
`/Volumes/Data2`, which is a separate physical drive with far more room.
What that buys is *decorrelation*: Data1 previously held 1.5 TB of media and
every backup, so one drive failure took both at once. Now one event takes
one thing. Be clear about what it does not buy — the two drives share an
enclosure, so a controller or PSU failure still takes both, as does theft,
fire, or an accidental delete. Restic to B2 remains the actual second
copy; this is a cheap improvement on the way there, not a substitute.

**Not needed yet:** a Zigbee/Thread coordinator only matters if Home
Assistant grows past the Hue bridge into Matter devices. And the mini's
mDNS flapping between its two interfaces is a config problem (a DHCP
reservation, or disabling the unused interface — see "Router as code"),
not a hardware one.

## Deliberately deferred

### Self-hosted secrets (OpenBao)

Moot twice over: the vault exit (#10) fixes the vendor
question while keeping a cloud origin *by decision* — the server side is
only ever a replica, exactly to avoid the circularity that parked
OpenBao here. The section stays as the record of why.

The original goal was cutting the cloud dependency. The `create_` template
pattern already achieves the operative part: `op` is a bootstrap
dependency, not a run-time one. 1Password Connect left this section
for the config plane (#10), pulled by a different goal —
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
