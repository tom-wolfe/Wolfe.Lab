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

### 11. Observability — OpenTelemetry into Grafana

The lab can say whether things are up (Gatus), how hard the machines are
working (Beszel) and whether the schedules are alive (the heartbeat). It
cannot say *why*: no logs outside `docker logs` and scattered files, no
traces, no application metrics. The mail watcher is the standing
example — slow, and nothing to say where the time goes. And #6 is
heading towards handing deployment to an agent — `lab` reconciling each
node from the repo — which is not a responsibility to hand over blind.
By the ordering principle this goes first: it makes failures visible,
where the agent is a new thing to fail.

**Decided: OpenTelemetry for every signal, Grafana's stack to read
them, Alloy to collect, seven days kept.** OpenTelemetry is the part that lasts — the instrumentation
outlives whichever backend receives it. Grafana over a single-binary
store such as OpenObserve: more to run (Loki for logs, Tempo for traces,
Prometheus for metrics, Grafana in front), but it is the standard shape,
the one that carries over to work, and its dashboards are the best on
offer.

**The shape.**

- **A collector on every node** — Grafana Alloy, a platform component
  deployed like any other. It receives OTLP from applications, reads container logs through the
  Docker API and host-process logs from their files (ollama, the
  runners, the Beszel agent), and scrapes the Prometheus endpoints
  services already expose — caddy, Forgejo, Garage, Gatus, Immich.
  On the Studio it follows the hybrid rule: nothing waits on it.
- **The backend on the mini**, behind caddy, tailnet-only. Loki and
  Tempo keep their data in Garage; Prometheus in its own store. Every
  signal is kept **seven days** to start, extended when a question needs
  more. Telemetry is disposable and is not backed up; datasources,
  dashboards and alert rules are provisioned from the repo, so a rebuild
  loses history and nothing else.
- **Grafana alerts; Gatus watches Grafana.** Alert rules over the
  metrics and logs — a filling drive, a container restarting, a job
  slowing down — go to Pushover like everything else. Grafana shares the
  mini's fate, and a watcher must not share the fate of the thing it
  watches, so the layers outside it stay: Gatus on the Pi pages when the
  mini, or Grafana itself, stops answering — a dead alerting engine
  looks exactly like a quiet night — and healthchecks.io stays the one
  observer outside the building.
- **Beszel retires.** Its host and container stats are what the
  collectors report, and its threshold alerts become Grafana rules; once
  both are in place, the hub, the agents and Gatus's check of the hub
  go.

**Why Alloy rather than the upstream Collector.** Both speak OTLP, so
the applications do not care, and a later switch is the collectors'
configuration alone. What decides it is container logs on the Macs:
Docker Desktop keeps them inside its VM, out of reach of a collector
reading files on the host, and upstream has no receiver that reads them
through the Docker API, where Alloy does. Alloy also ships the Loki and
Prometheus pipelines natively, has a UI for debugging a pipeline, and is
what Grafana's own documentation assumes.

**The .NET side is cheap.** The mail watcher already runs on
`Microsoft.Extensions.Hosting` and `Microsoft.Extensions.AI`: its logs
go out through the OpenTelemetry logging provider, HttpClient emits
spans by itself, and the AI client's `UseOpenTelemetry()` gives a span
per model call with its duration and token counts — the likeliest
bottleneck, measured.

**Ritten emits traces.** Its model is already a trace: a run is the
trace, a job its root span, each step a child span carrying its kind
and result. Instrumented in the engine, every CI run and every one of
the agent's reconciles becomes something to inspect, filter and
compare over time — the view the Actions tab gives today, for the thing
that will replace it. A Ritten feature, so a Ritten release, then the
lab's pins.

**In order.**

1. The backend on the mini and a collector beside it.
2. The mail watcher instrumented — the bottleneck question answered.
3. Container and host-process logs from every node.
4. The services' own metrics, scraped.
5. Host metrics and the alert rules that replace Beszel's; Gatus
   watching Grafana; then Beszel removed.
6. Ritten's traces — before the agent, so it is observable from its
   first reconcile.

**Still to decide:** how much of #9's dashboard half Grafana answers by
itself.

### 6. CI/CD — a kernel, and an agent that deploys the repo

Where it stands: every node runs a **host** runner for the lab's own CD —
repo-scoped, holding the node's full vault token — and every component
deploys as a job on it. The Pi also runs a **containerised** runner (the
`docker` label: no host, no vault, no socket in a job) where CI runs.
It works, and it has four costs:

- **Every deploy is a host job with the whole vault**, and a branch can
  send a job to any host runner — so write access to Wolfe.Lab is, in
  effect, the whole platform. The tofu checks are the sharp end of it:
  pull request code, planned on the mini, with every secret.
- **Pushing is the only way anything converges.** Nothing notices drift;
  a node that missed a deploy stays behind until the next push.
- **Slices deploy one at a time, but some config belongs to the whole
  repo.** Every slice carries a `caddy.caddyfile`, and the front door's
  hook has to gather them; Gatus checks, the collectors' scrape targets
  (#11) and backup schedules are the same shape.
- **A slice does not own its own deployment.** Where a component runs is
  a `runs-on` in a workflow file under `.forgejo/`, not a fact of the
  component, and a node is onboarded by hand.

**Decided: a small kernel deploys an agent, and the agent deploys the
repo.**

- **The kernel** is what the agent needs in order to run: the machine as
  chezmoi declares it, the runners, Forgejo, the vault, and the agent
  itself. It deploys the way everything does today — host runners, and
  `lab init` for a new node. It is small on purpose.
- **Everything else is the agent's**: services (Immich, Jellyfin, the
  *arr stack, Paperless, mail) and platform services alike (caddy,
  Gatus, Garage, the collectors, Grafana). Pipelines build, check and
  publish in containers and never touch a host; the agent pulls. The
  pattern from AWS and Azure — and Kubernetes' too, where an ingress
  controller is an ordinary workload configured from every app's
  Ingress.

**`lab` as an agent.** A daemon on every node — the mail watcher's
.NET hosting pattern, installed by the kernel — that deploys *the repo
at a commit*, not a slice at a time, with the deploy code the CLI
already has:

- **A component declares where it runs**, in its own `ritten.json`, so
  "what runs on this node" is a question the repo answers; the per-slice
  deploy workflows go, and each slice owns everything about itself.
- **Cross-slice config is assembled, not gathered**: the agent reads
  every slice's routes at the commit, renders caddy's config once, and
  reloads only when it changed. Checks, scrape targets and schedules the
  same way.
- **A merge is the desired state, a revert the rollback**, and drift is
  noticed because the agent keeps looking.
- **It resolves each component's secrets itself**, which makes it the
  place to narrow what each node can read (#8).
- **It reaches Forgejo directly** — loopback or the tailnet, never
  through caddy — so it never depends on anything it deploys: a broken
  route cannot cut it off from the fix.

Two things make it safe to hand deployment over: **it is observable
from its first reconcile** (#11: each reconcile a trace, each step a
span), and **it reports where the Actions tab does** — a commit status
per node on the commit it converged, so a deploy still goes green or red
in Forgejo.

**Tofu: planned on the pull request, applied after merge, both by the
agent.** Atlantis's shape. The agent watches pull requests rather than
exposing an API — nothing to call it, so nothing to authenticate — and
for one that changes a tofu root it plans the head commit and posts the
plan as a comment and a status; on merge it applies. Be exact about what
this buys: a plan runs the pull request's code with that root's
credentials — providers are binaries it can change, `external` and
`http` data sources can run and send anything, and state (which holds
secrets) is decrypted to read it — so no one can plan untrusted code
safely. What changes is the reach: today a bad pull request gets a host
shell and the whole vault; with the agent planning, it gets at most the
one root it changed, and the CI job holds nothing at all. The agent can
narrow it further by refusing to plan what it cannot trust — an author
other than Tom, a changed provider or lock file, a new `external` or
`http` data source — posting "needs review" instead.

**CI is one workflow.** With deployment gone from workflow files, what
is left is checking: one workflow in the containerised pool that finds
the components a pull request changed and runs each one's `lab check`
(the path filter gate already does half of this). The next best thing
to pipeline files living in their slices.

**Agent mode is a Ritten feature.** Today the engine runs a job once;
an agent runs the same job again whenever the desired state moves. The
step model, rules and reporting carry over — a Ritten release before the
lab's agent can exist.

**`lab init` onboards a node.** `dotnet tool install -g
Wolfe.Lab.Build`, then `lab init`: apply chezmoi for the machine's
profile, register its runners, install the agent. The reverse tears a
node down — deregister, stop, remove — so a node is one command either
way. On a laptop, `lab init` is only the chezmoi apply, uncommitted to
anything. This is also where the runners move out of chezmoi, which
retires the Full Disk Access debt below. `setup.sh` stays the cold
start: `lab` comes from Forgejo's feed, so Forgejo exists first.

**The mini joins the build pool.** The containerised runner as a
component, identical on every node — the runner image, socket-mounted so
it can start sibling job containers. On the Pi it replaces the
systemd-supervised second runner. One mechanical detail: a runner in a
container hands job containers bind mounts by host path, so its work
directory must be the same path inside and out; Forgejo's own compose
example does exactly that.

**The trust boundary.** Once the agent carries everything but the
kernel, the host runners deploy only the kernel, and its workflows could
live in a repository of their own that only Tom can push to — so write
access to Wolfe.Lab (a friend's, Renovate's) stops meaning the platform.

Kubernetes and Argo CD were considered: Argo is exactly this reconcile
loop, but it only targets a cluster, and the Macs are not going to host
one. A learning item, not the plan.

**In order.**

1. Observability (#11) — the agent is not built blind.
2. The mini joins the build pool.
3. `lab init`, and the runners out of chezmoi.
4. Placement in the components' declarations — `runs-on` read from the
   component, while the workflows still deploy.
5. Ritten's agent mode, then the lab's agent: one low-stakes service
   first, then the rest, then caddy's assembled routes.
6. Tofu through the agent — plans on pull requests, applies on merge —
   and CI as one workflow.
7. The trust boundary.

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
that saves opening four of them. Grafana (#11) may answer much of this
half by itself; decide after it lands.

### 10. Asking the vaults questions

The Obsidian vaults as something a model answers from, at the desk:
ollama plus a RAG layer over a clone of the vault repositories on
Forgejo (`obsidian/`), not a mount. The corpus is already versioned and
already synced by workflows, so the index job is an ordinary `lab` job
with a clean input.

What it stands on is in place. `lab/embedding` is the same model on
both servers, so an index built on either is searchable from either,
and the index can be a nightly job on the mini like everything else
scheduled. `lab/interactive` is the Studio's model while it is on — a
person is waiting, so a question may use most of the machine for as
long as it takes — and the mini's while it sleeps: worse answers, not
none (`ollama/README.md`, "Roles"). Paperless already runs the same
shape over its documents, a nightly embedding index behind a chat
(`paperless/README.md`, "AI"); worth learning from before building.

Still to decide: the front end — Open WebUI in front of `ai.twolfe.dev`
is the cheapest start, an MCP server for the vault the durable shape
("The AI layer", below) — where the index lives and how it is backed
up (or whether it is simply rebuilt), and whether a bigger interactive
model earns its memory on somebody's workstation.

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

### The mini's runner loses Full Disk Access on every Homebrew upgrade

The fix is a signed copy of the runner at a fixed path, so the grant
survives an upgrade that moves Homebrew's binary: a step of the runner
component once the runners' install moves out of chezmoi into the CLI
(#6, `lab init`; registration already has, `forgejo/runners/`). Until then the grant is
re-given by hand after an upgrade. The Studio's runner holds no grants,
so it has no such problem.

### The Studio wakes only by hand

A job for the Studio queues while it sleeps and runs when it next wakes
(`forgejo/README.md`, "The Studio's runner"), which is enough while
nothing is urgent. A Mac cannot be woken from *off* over the network,
and a cold boot behind FileVault stops at the login window before any
LaunchAgent starts; from *sleep*, with "Wake for network access" on, a
Wake-on-LAN packet from the Pi brings it back with the session intact.
So "switched off" means asleep, and a wake step in the workflows that
target it is there to add if a queued deploy ever needs to land sooner.

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

**The bigger reason is events, not the devices.** Presence from the
companion app, time, device state: HA is where "something happened"
becomes an automation. It is a *participant* on the lab's event bus
(below), not the bus itself: it speaks MQTT natively, which the broker
can carry beside AMQP. It ships an Ollama conversation integration, so
`ai.twolfe.dev` can be its assistant with no new service. The aircon and the TV join the list
above; whether either has a *local* integration depends on the brand, and
is worth checking before assuming it.

### The personal data plane — out of the walled gardens

Most of the items below are the same move. Asking questions — of
the vault (#10), the inbox, the calendar — only works over data the
lab can read, and today calendar, contacts, tasks, messages and
audiobooks each sit in an app that will not talk to anything. Each item
here stands on its own, but the order is set by how much it unblocks
for the others. **Everything stays tailnet-only**; none of these is a reason
to expose the lab to the internet.

### Calendar and contacts — Radicale

The highest-leverage item in this section. A CalDAV/CardDAV server
(Radicale, or Baïkal) as the **source of truth for contacts**: import
from Google and iCloud, dedupe once, point every device at it. iOS and
macOS speak both natively. Radicale's storage is plain `.ics`/`.vcf`
files with a git hook that commits every change, the same "state is
files" property this repo prefers everywhere else.

What it buys the mail watcher: events written directly, without Proton
Calendar's restrictions. What it costs, named up front:

- **Invitations.** Radicale does no server-side scheduling (RFC 6638),
  so sending an invite to someone else is not its job; Baïkal does it
  over mail. Worth deciding before choosing between them.
- **Proton cannot subscribe to it.** Proton fetches subscribed calendars
  from its own servers, and a tailnet-only URL is not reachable from
  there. Realistically the calendar front end becomes Apple Calendar
  over this server, with Proton kept for the invitations it receives.
- **iOS polls CalDAV**; there is no push. Fine for a calendar, and worth
  knowing.

It also serves VTODO, which iOS Reminders reads for a CalDAV account.

### An event bus — RabbitMQ, with the mail watcher as its first producer

The mail watcher today does one thing end to end: mail in, appointment
out. The want is wider: train tickets, hotel stays and, above all,
gigs, and the right shape for that is producers and consumers on a
broker, not one process growing a branch per event type. Tom has built
this shape professionally (Service Bus, RabbitMQ), and AMQP for the
watcher is already the plan.

**RabbitMQ**, because it speaks AMQP to the lab's own services *and*
MQTT, through its plugin, to Home Assistant, so one broker covers both
and HA is a participant rather than a second bus. A compose slice on the
mini, tailnet-only.

The shape, roughly:

- **Producers** publish facts: the mail watcher (a classified message:
  appointment, train, hotel, gig ticket, grocery order), the dictaphone,
  the Monzo feed (a transaction), HA (presence, device state).
- **Extractors** turn a classified message into a typed event, one per
  kind, so adding hotels is a new consumer, not a change to the watcher.
- **Sinks** project events into the places they are read: Radicale, a
  commit to the vault, KitchenOwl, a Pushover message, HA.

**Replay is the property to design for.** When a new extractor lands, it
should be able to see the mail that arrived before it. A RabbitMQ
*stream* for the raw classified messages gives that; a new consumer
reads from the start. The mailbox stays the real origin, so the broker's
own state is re-derivable and, like `mail/bridge`'s volume, needs no
backup section.

On the client side: MassTransit moved to a commercial licence from v9;
v8, `RabbitMQ.Client` directly, or Wolverine are the options. Worth
checking where that stands when this starts.

### Gigs — the friend group's tickets, and the history in Obsidian

The problem is organisational, not technical: who is going to which gig,
who holds whose ticket, who has paid whom. The Google Sheet fails for
two reasons. **Nobody checks it**, because it is somewhere nobody looks, and **it is
filled by hand**. Both are fixable once ticket emails are events.

**Capture: ticket emails.** Ticketmaster, DICE, See Tickets, AXS and the
rest send confirmations with artist, venue, date, ticket count and price.
A gig extractor on the bus turns them into gig events; nobody types
anything for the tickets Tom buys, and a quick form (or the dictaphone) covers
"a friend bought four for Friday".

**The gig store is the lab's; everything else is a projection of it.**
The same event projects to three places, each where its reader already
looks:

- **A shared Google Calendar the friends subscribe to.** Nobody checks
  the Sheet, but everybody checks their calendar. The lab writes the
  event and who is going lives in the description, or as attendees.
  It has to be a Google (or iCloud) calendar rather than Radicale,
  because the friends are not on the tailnet and the lab is not
  exposed. The Sheet can be retired, or kept as a read-only projection
  for anyone who liked it.
- **The vault**, as today but automatic. A gig note (and an artist note
  if it is the first time) committed on purchase, with frontmatter
  Obsidian Bases already reads for counts and upcoming gigs. After the
  date, **setlist.fm's API** (free for non-commercial use) attaches the
  setlist, and MusicBrainz IDs keep artist notes from splitting on
  spelling.
- **A reminder**, pushed shortly before the gig, with who holds which ticket.

**Who has paid whom.** Ticket price per head comes from the email.
Repayments arrive in Monzo, which is already on the bus through the
Sheets feed, so a transfer from a friend with a recognisable reference
can mark their share paid. What is left unpaid becomes a nudge, not a
Sheet cell. Worth checking before building: whether Splitwise (it has an
API), or Monzo's own shared tabs, already covers the ledger half well enough to be the
sink instead.

**Honest boundary:** posting into the friends' WhatsApp group is the
place they would actually see it, and WhatsApp has no supported way for a
bot to post into a personal group. The shared calendar is the substitute.

### Ebooks — Calibre-Web Automated

A Humble Bundle library of PDF, EPUB and MOBI, a Kindle and an iPad, with
nothing between them. CWA on the mini, library on the data drive: files
dropped into an ingest folder get imported, converted (MOBI → EPUB) and
their metadata fixed. **Send-to-Kindle by email** is the join: through
`mail/bridge`'s SMTP (the sender address approved in Amazon's
settings), a book reaches the Kindle *and* the Kindle app on the iPad,
with reading position synced across both. Amazon no longer accepts MOBI
this way, so the conversion is required, not optional. Image-heavy technical
PDFs stay on the iPad, through any OPDS reader (Readest, for one).
Booklore is the newer alternative (Kobo and KOReader sync, a better UI)
and is less proven; worth a look when this starts.

### Audiobooks — Audiobookshelf, and Libation to own the Audible library

Audiobookshelf is the self-hosted answer, with a good iOS app. **Libation**
is the more interesting half: it downloads purchased Audible titles as
DRM-free `.m4b`. Audible stays the storefront, and the library becomes files
that restic covers and Audiobookshelf serves. Its CLI as a scheduled `lab`
job is the shape.

### WhatsApp and iMessage archive

The only WhatsApp history is inside iCloud's WhatsApp backup, which is
opaque. A **local encrypted iPhone backup** contains WhatsApp's
`ChatStorage.sqlite`: `idevicebackup2` (libimobiledevice) on the mini
can take one over the network, **WhatsApp-Chat-Exporter** turns it into
searchable HTML/JSON, and **imessage-exporter** does the same for Messages.
This is an archive *beside* iCloud's device backup, not a replacement.
iCloud stays the full-device restore path, and this is also the
answer to "the iPhone is backed up only to iCloud". Caveat: network
pairing with libimobiledevice is known to be flaky, so a manual
quarterly backup over the cable may be the realistic version.

### Things — integrate, don't migrate

Nothing self-hosted has an iOS app as good as Things, and it is not a
subscription. It is less closed than it looks. **Mail to Things** (a private
address that turns an email into an inbox to-do) is a write path the
mail watcher and the dictaphone can use through Bridge, and the Mac app
keeps a **local SQLite database** that can be read from any Mac where it
is installed. Read one way, write the other; no migration.

### Shopping list and groceries

Things is the shopping list today, and a to-do app is the wrong tool for
it. **No UK supermarket has a usable API**: Tesco's developer
programme closed years ago, and Sainsbury's and Waitrose never had one. Every
"add my list to the basket" tool is scraping or browser automation. That
is fragile, against the terms, and one site redesign from silently wrong.
Automating *checkout* is off the table either way.

**Decided: KitchenOwl**: self-hosted, a native iOS app, a shopping list
that learns items and orders them by aisle, plus recipes and a meal
plan that feed the list.

**The recipes' origin stays the vault.** They are Markdown in Obsidian
today and stay there. A `lab` job syncs vault → KitchenOwl one way,
through its API, keyed on the note path, so editing in KitchenOwl is
not the workflow and a resync is always safe. That needs one
convention in the vault: ingredients as a parseable list with quantities
and units (frontmatter or a fixed heading), which is worth settling
before the sync is written.

**Order history is the part worth building**, and the same kind of job as
the event scanner. Online order confirmation emails are itemised and
already arrive in Proton; parsing them gives what no supermarket exposes:

- **Frequency**: "you buy this every ten days, it's been twelve", as
  suggested list items.
- **Price**: actual prices paid per product, over time. With a saved
  link per preferred product (the supermarket can't be automated, but a
  list of *which* product to buy can be kept), a list or a week's meal plan
  gets an estimated cost before the shop.
- **Backfill**: a Clubcard or Nectar data request (GDPR) for history
  that predates the parsing.

**Nutrition, two sources.** Per *product*: Open Food Facts (free API,
barcode-keyed, decent UK coverage), matched to the saved products. Per
*recipe ingredient*: CoFID, the UK government's composition-of-foods
dataset, as a local table. Recipe nutrition is then arithmetic over the
ingredient list, which is one more reason to settle that convention.
Supermarket product pages carry nutrition tables too, but reading them is
scraping; the open datasets come first.

**Basket filling by browser automation** stays an experiment at most:
a list in, a basket filled, a person reviews and checks out.

### Finances — Monzo

All banking is Monzo. Budgets are *set* in Monzo, which does that well;
what is missing is reporting and analysis, so that is the whole scope.
No budgeting app (Actual, Firefly III); they would duplicate what Monzo
already does.

**The feed is the Google Sheets export, not Monzo's API.** The Sheet
updates live with every transaction, and the Sheets API v4 reads it with a
**service account** the Sheet is shared with read-only: a static key in
the vault, no OAuth dance, no rotation. Monzo's own developer API would
have shaped the design badly: personal use only, full history readable
only for a few minutes after an in-app approval and 90 days after that,
and a refresh token that rotates and so needs writable state the vault
cannot hold. The Sheet sidesteps all of it. The costs: a Google
dependency, and the export being a paid-plan feature.

- **A scheduled `lab` job polls the Sheet into a local SQLite ledger**,
  upserting on Monzo's transaction ID. Upserting, not appending: rows
  change in place when a transaction is recategorised or settles.
  The ledger is the lab's copy, and the Sheet is only the feed.
- **Pots and pot transfers** make naive spending sums wrong; handling
  them is most of the real work.
- **On the event bus** (below), each new transaction is an event. That
  is what lets the gig tracker mark a friend's repayment as paid.

Then the analysis half: a read-only query surface (an MCP server over
the ledger) for the same front end as the vault, and a dashboard for
the reports Monzo does not have. **Financial data only ever goes to
local models.** Joined with grocery order history, it also answers
"what does food actually cost us".

### STL library — Manyfold

A paid-for collection of D&D miniature STLs outlived the SLA printer.
Manyfold is a self-hosted 3D model library built for exactly this:
thumbnails, tags, collections, pointed at the existing folders on the
data drive, so nothing moves. It becomes more useful if an FDM printer
arrives for rack parts. When choosing one, prefer a real local API (Prusa,
or anything on Klipper/Moonraker). Bambu's LAN mode works, but its 2025
firmware authorisation dispute is worth reading first.

### forScore — leave it, back it up

Nothing self-hosted comes close for sheet music on an iPad. The only gap
is that the library lives in iCloud alone: a periodic forScore backup
file (or PDF export) landing in a path restic covers closes it.

### The AI layer — dictaphone, and asking questions of everything

What the models are for, beyond the mail scanner and Paperless.

**The dictaphone.** Action button → Shortcut → *Record Audio* → *Get
Contents of URL* (multipart POST to the mini over Tailscale) →
transcription (`whisper.cpp` or `parakeet-mlx`) → an LLM classifies the
transcript, reusing the mail watcher's event detection. Notes become a
commit to the vault repo, tasks go to Mail to Things, events to Radicale,
and shopping items to the list. Whisper-family transcription waits
for this. Design for the phone being off the tailnet:
the Shortcut saves to an iCloud Drive folder as a queue rather than failing.

**Asking questions.** Open WebUI in front of `ai.twolfe.dev` is the
cheapest start. The durable shape is **one small MCP server per source**:
the vault, CalDAV, IMAP through Bridge, Things' SQLite, Paperless's API,
the finance ledger. Any front end and any model can then use them, local or
not (with the finance caveat above). On the Studio, in the interactive role.

**Smaller things that fit:** paperless-gpt (Ollama-generated titles and
tags for Paperless), Karakeep (read-later with AI tagging), SearXNG (web
search for local models), Dawarich (location history, a Google Timeline
replacement and another event source), an Atuin sync server (shell
history across the Macs).

**A suggested order**, by the ordering principle: Radicale → the event
bus, with the mail watcher moved onto it → gigs (the first new extractor, and
the one with a real problem behind it) → Audiobookshelf with Libation,
and CWA → the dictaphone → Home Assistant → the Monzo feed and the MCP
servers → KitchenOwl and order history. Manyfold, forScore's backup and the message archive whenever
you like; nothing waits on them.

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
