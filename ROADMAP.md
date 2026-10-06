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
in place: each service's `lab restore-drill` restores its `backup.verify` paths
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

### 13. The primary node becomes a Linux box — the DXP480T Plus

Bought 2026-09-30. The reason to move is still the physical one: **a Mac
cannot host a storage controller.** No Mac takes internal drives or an HBA,
so every disk the lab owns arrives through a bridge chip in an external
enclosure. Today that is a 3U Sabrent DAS with four 3.5" bays, two of them
used. The VM boundary ("The container runtime") is the other half: host
networking, port 53 and the first-boot sharing races all go once containers
run natively.

**The machine is a UGREEN NASync DXP480T Plus**, not the specced N305
build. That build was a mini-ITX NAS board, a 2U case, an IcyDock cage and a
Pico-PSU, about £1,000 and all self-assembly. This is £730 and none:

- **i5-1235U**, 10 cores and 12 threads, well past what the platform layer
  needs; the heavy work still goes to the Studio.
- **8 GB DDR5, two slots with one free, 32 GB max** by the spec sheet. A
  16 GB stick in the free slot is the next purchase.
- **Four M.2 NVMe slots and a separate 128 GB system SSD**, so all four
  slots stay data. UGOS Pro comes off (save its image first); Linux goes
  on the system SSD.
- **10GbE and two Thunderbolt 4 ports.**
- **178 × 142 × ~50 mm**, so a 2U shelf in the 10" rack.
  `assets/models/nasync-dxp480t-plus-mount.3mf` is a mount for it.

**Storage is NVMe, and one drive to start.** A 4 TB Samsung 990 Pro is the
first data drive, with more NVMe as money allows. One drive has no
redundancy: until a second mirrors it, the recovery plan is the local
restic repository plus B2. That is a decision, not an oversight.

**The HDDs stay, as the backup tier.** The Sabrent holds a Seagate
BarraCuda 2 TB (7200 rpm, bought 2023-09) and an IronWolf 4 TB (5400 rpm,
bought 2025-05). They move to a 1U shelf that takes two 3.5" drives flat,
on USB-SATA adapters into the node, and hold the local restic repository.
Backups then live on a different device from the data, which is the
decorrelation Data2 was bought for. Spin them down when idle, since backups
run nightly, and SMART-check the BarraCuda before trusting it. The Sabrent
is decommissioned, which frees 3U and a socket (see "Rack power" under
Hardware).

**The bundled US3000 is not the rack UPS.** A UGREEN 120 W DC UPS came with
the node: 43 Wh, inline between the node's brick and the node, zero
transfer time. It signals shutdown to UGOS Pro over USB. Whether NUT sees it
under Linux is unverified, and without that it runs until empty and then
cuts, which is the failure the UPS exists to prevent. Test it during
burn-in. Either way it covers the node and nothing upstream of it.

**The backup transport gets revisited here, by its own instruction.**
`restic/README.md` declines a `rest-server` on the mini because sshd is
native and the drive is native, and says to revisit "when the platform
layer moves to Linux and the drive's host changes", which is this. An
append-only mode and a path jail are the gain. A container in the backup
path is the cost, and on Linux it is no longer a container behind a VM.

**The filesystem is a decision, not a default.** ext4 or xfs is the simple
answer. ZFS or btrfs adds checksumming and scheduled scrubs, which is the
only thing that would ever notice bit rot in the media library. The library
is deliberately unbacked and today watched by nothing, because restic
verifies its own repository and not files it was never given. A second
NVMe makes a ZFS mirror the natural next step.

**Sequence.** APFS forces the order. Linux reads it only through unreliable
read-only tooling, so nothing is re-seated: data is copied across the
network from the mini, and a drive is wiped only once it is empty. Every
step is safe to stop at, and the lab keeps running on the mini throughout:

1. Build and burn in with no lab data. Linux on the system SSD, memtest, a
   stress run, and the US3000 against NUT while pulling its plug costs
   nothing. The 8 GB it ships with is enough for this.
2. Make it a node that does nothing: chezmoi profile, Tailscale, host
   runner, Beszel agent, Gatus check. The mini is untouched and still
   primary; this proves the machine before anything depends on it.
3. With the 990 Pro in, migrate services one at a time, Forgejo last because
   it runs the deploys. Each keeps its `backup` section and takes the
   node's own restic path (`restic/README.md`, "From a Linux node").
4. Media onto the 990 Pro. Then the HDDs, one at a time: wipe the emptied
   media drive, reformat it, and copy the restic repository onto it. Run a
   restore drill from B2 **before** wiping the second drive, because for
   that window B2 is the only other copy.
5. The mini stops being primary.

Step 3 restores each service's state onto new hardware and boots the service
on it. That **is** the boot-on-restore drill #1 asks for, provided it goes
through `lab restore` rather than by hand, so the migration retires that
half of #1 instead of deferring it.

**The Pi keeps its roles.** It runs the containerised CI runner and Gatus.
Gatus should stay off the primary, since it is what notices the primary is
down. Home Assistant and Pi-hole are designed for the Pi because they want
real host networking. The node has that too, so either can host them.

**What the mini becomes.** Not a server, and that is the point: it stops
being the one machine the lab dies with. What genuinely cannot move:

- **An Apple-platform build runner.** Xcode, codesigning and notarization
  are practical only on Apple hardware. It slots in as another `runs-on`
  label with no new architecture.
- **macOS VMs for bootstrap testing.** Virtualization.framework runs only
  on Apple silicon, and the `macbook` / `work-macbook` / `macmini-node`
  profiles have no test target today short of wiping a real machine.
- **Apple-local data, if the assistant grows past Obsidian and mail.**
  Messages, Notes, Contacts and Photos live in `~/Library` on a logged-in
  Mac. It needs Full Disk Access, so it carries the TCC debt, now confined
  to a machine nothing depends on.

Everything else moves.

### 14. A domain model, and the lab described in its own files

Groundwork for the agent (#6), the config plane (#8) and the portal (#9),
all three of which assume something the lab does not have yet: a typed
description of what it is. Today what a component *is* is spread across
formats, each read by code of its own:

- **`ritten.json` binds per workflow.** Eighteen workflows over about fifty
  components, each with an options type of its own, so the same idea is
  declared again in each: `volumes` in four of them, `release` in four,
  `image` and `images` in five, `heartbeat` in two and again in tofu.
  Nothing knows they are the same thing.
- **Cross-cutting facts are gathered, in whatever format they were
  written in.** Routes are `caddy.caddyfile` snippets that another
  component collects, health checks live in Gatus's central YAML, a log's
  location is an agent's `log` or a compose label, and service metrics
  were about to be a fourth shape. Each needs a scraper of its own, and
  the telemetry checks show where that leads: untyped YAML walked key by
  key, and problems returned as bare strings.
- **Declarations repeat rather than compose.** The Alloy agent is the
  same package, program and arguments once per node — 94 lines for one
  agent.
- **There is no domain layer.** `Clients/` is roughly infrastructure and
  the workflows, jobs and steps roughly the application, but the model
  between them is `Values/`, a folder of helpers rather than the
  foundation the rest stands on.

**Decided: a `Wolfe.Lab.Domain` project, and lab-owned YAML declarations.**

- **The domain is a project of its own**, not a namespace, so the
  dependency direction is the compiler's to hold: it depends on nothing
  but Ritten's `Result` and `Error`, and its file system abstraction
  (`IDirectory`, `IFile`) — the one way it touches a disk, and the one a
  test fakes. It holds the
  model — areas, services, components, nodes and their roles — the value
  objects, now foundational rather than helpers, and the rules between
  them ("a scraped service is one its compose project has"). The
  infrastructure reads files and talks to Docker, launchd, systemd and
  the vault; the application orchestrates. A second benefit: the agent
  (#6) becomes a project of its own over the same domain, and the CLI
  stays intact beside it.
- **The lab describes itself in files of its own**, YAML, beside what
  they describe, rather than in `ritten.json`, which is Ritten's: each a
  typed document the domain reads, small because each says one thing.
  Datadog's service catalog is the model — a service as the unit people
  think in, with what it is, who to read about it and where its
  dashboards and runbooks are — rather than a deployment tool's manifest.
- **Area, service, component** is the vocabulary, the label schema's
  already (`lab.area`, `lab.service`, `lab.component`):
  - an **area** classifies — `media`, `monitoring` — and is the top
    directory, with no declaration of its own;
  - a **service** is the unit the catalog lists: a `kind: service`
    document in its directory, reversing platform/lab/README.md's "a
    service never carries a declaration of its own", since its name, what
    it is and its links belong to the service, not to any one of its
    components;
  - a **component** is a logical part of a service — a process the lab
    runs, the database it keeps, a directory of its state — never a
    deployment unit: Immich is a server, a machine-learning worker, a
    cache and a database, not "a compose stack". Its `kind:` says what it
    is used for and its `workflow:` what operates it — `kind: database,
    workflow: docker` — and `partOf:` the component it lives inside, if any.

  *Slice* retires: it was the repository's word for a service's
  directory, beside the telemetry's `lab.service` for the same thing,
  and two names for one idea is one too many.
- **Native files stay native.** `compose.yaml`, the Caddyfile and the
  collectors' configs are what Docker, Caddy and Alloy read, and say only
  what those need. The domain reads them only to hold a declaration to
  them — compose's normalised form, `docker compose config --format json`,
  rather than its YAML — and never scrapes the lab's facts out of them.
- **The end state** is a lab described as a set of metadata files, a
  portal (#9) that shows them, and an agent (#6) that operates from them.
- **The agent is the lab's, not Ritten's — at first.** Built bespoke,
  over the lab's own domain, until it is clear what works; only then is
  it worth asking whether any of it generalises. Ritten stays what it was
  started as — CI/CD for simple linear workflows, for work and for open
  source builds — and a kit for building one's own pull-based deployment
  agent has no use outside a homelab. This reverses #6's "agent mode is a
  Ritten feature".
- **Ritten runs workflows; the agent orchestrates them.** Ritten stays
  the runner of a workflow's steps, with their rules and reports; the
  agent decides what runs, where and when, and automates Ritten to do it.
  `ritten.json` goes: a lab job declares `RequiresProject => false` and
  takes its component from the lab's declarations rather than from a
  project file, so a component is described once.

**Decided: the documents.**

Every document says what it is with `kind`: `service`, `node`, or one of
the kinds of component below. By convention a service's is
`service.yaml` and a component's `component.yaml`, but the file's name
decides nothing (see "found by its schema").

- **A service** is its catalog entry: `name`, an
  optional `displayName`, `description`, `lifecycle`, `links` (the app,
  its runbook, upstream docs, dashboards) and `dependsOn`, other
  services. It passes nothing operational down to its components:
  explicit beats inherited.
- **A component** has the same `name`, `displayName`
  and `description`, its `kind` and `workflow`, `partOf` and `dependsOn`
  (other components of its own service), what its workflow needs to act on
  it — a `docker` component's compose service, a `sqlite` one's path — and
  its **facets**: sections that mean the same wherever they appear —
  `runsOn`, `requiresVolumes`, `artifacts`, `packages`, `logs`,
  `metrics`, `backup`, `route`, `check`, `heartbeat`, and the rest the
  migration finds. Which facets a component may carry is its workflow's to
  say (below).
- **`name` is the identity**, a slug unique within its service. A file may
  hold several documents, split by `---`: a directory is needed only where
  native files live — a compose stack's components are documents in one
  file beside its `compose.yaml`, and three vault backups, or a dozen
  once the D&D vault is one per campaign, documents in one file rather
  than directories. Every document names itself — no name is read from a
  directory — so a declaration says what it is called where it says it.
- **`partOf` is a component's place inside another, recursively.** A
  SQLite database is part of the app whose state holds it; a Postgres
  database part of the Postgres server holding it, itself a `compose`
  component. `partOf` names a sibling — within the service, never a
  cycle — so declarations stay flat while the parts nest: a diagram nests
  by it, and whatever a backup must stop is found by walking up it to the
  nearest component the lab runs.
- **A service is the aggregate its components belong to.** A component
  exists only as its service's, which holds the rules between siblings —
  unique names, dependencies kept inside the service — and a service
  holds its area, which its components read rather than copy. The rules
  between services (unique names, `dependsOn`) are the catalog's. So a
  deploy finds its component in the catalog, declared with its service,
  and the placement read from a path alone is gone.
- **Like depends on like, and never across a service's boundary to a
  component.** A service depends on services, a component on components
  of its own service. What one service needs of another is published
  through #8's config plane, a contract both sides can see — not a
  reference into its components, which the depended-on side cannot know
  it has and would break by changing.
- **Telemetry attributes are derived, never declared.** `lab.area`,
  `lab.service` and `lab.component` come from where a document sits and
  its `name`; `host.name` and `lab.role` from the node. No attribute has
  come to mind that is neither derivable nor better placed as a fact of
  its own, so a service declares none.

A sketch, to be held to as the schemas firm up:

```yaml
# personal/immich/service.yaml
kind: service
name: immich
displayName: Immich
description: The photo library, and the phone app's server.
lifecycle: production
links:
  - { title: Immich, type: app, url: https://immich.twolfe.dev }
  - { title: Runbook, type: runbook, path: RUNBOOK.md }
```

```yaml
# personal/immich/compose/component.yaml — beside Immich's compose.yaml
name: server
kind: app
workflow: docker
service: immich-server            # the compose service it converges
runsOn: [mini]
requiresVolumes: [/Volumes/Data2]
route: { host: immich.twolfe.dev, to: immich-server:2283 }
check:
  url: https://immich.twolfe.dev/api/server/ping
  every: 2m
  expect: { status: 200, body: { res: pong } }
metrics: { port: 8081 }
---
name: postgres
kind: database
workflow: docker
service: immich-database
---
name: database
kind: database
workflow: postgres                # a state workflow, to come with the backups
partOf: postgres
database: immich                  # dumped nightly: a job of its workflow
---
name: cache
kind: cache
workflow: docker
service: immich-redis
---
name: machine-learning
kind: model
workflow: docker
service: immich-machine-learning
```

```yaml
# personal/obsidian/vaults.yaml
name: main-vault
kind: backup
workflow: obsidian
path: ~/Obsidian/main
repository: http://macmini.local:3000/Obsidian/Wolfe.Main.git
---
name: dnd-vault
kind: backup
workflow: obsidian
path: ~/Obsidian/dnd
repository: http://macmini.local:3000/Obsidian/Wolfe.Dnd.git
```

**Decided: `kind` is usage, `workflow` is what operates it, and the two
are independent.** A component's kind says what it is for — the icon it
would have on a diagram — and its workflow what the lab does with it.
Neither implies the other: Redis is a `cache` in Immich and a `queue` in
Paperless, and Postgres, with its extensions, could be either too. Where
behaviour ever depends on both, it is a rule on the pair; no list of
permitted pairs.

- **The workflow is the type.** A closed, behaviour-bearing type was the
  set of workflows under another name, mapped back to them; so a
  component names the workflow that operates it, by the CLI's own name,
  and nothing maps one to the other. Its jobs are everything the lab does
  with the component — deploy, check, and for state a backup, a restore,
  a drill — and a test holds every name a declaration may use to a
  workflow the CLI has.
- **Both are closed sets**, grown by a code change — a new workflow is
  one anyway. Renaming a workflow renames it in every declaration.
- **`workflow` is the discriminator.** Each workflow's components are a
  document shape of its own — the options `ritten.json` holds today,
  moved into the declaration — read polymorphically, so a workflow says
  what it needs and which facets it takes: a `docker` component its
  compose service and `logs`/`metrics`, a `sqlite` one its path. The
  schema has a branch per shape rather than one that holds everything
  loosely.
- **The link to a runtime is the workflow's own field.** A `docker`
  component names the compose service it converges (`service:
  immich-server`), so a component's name is the catalog's (`server`) and
  the container keeps the unique name Docker Desktop and Beszel list. The
  deploy holds the two to each other exactly: every service of the
  stack is some component's, and every component's is a service the
  stack has.
- **While `ritten.json` remains**, a directory's components declare the
  workflow it runs; a part of one (`partOf`) names its own.

The first sets, each a starting point rather than a closed argument:

| `kind` — what it is for | |
| --- | --- |
| `app` | what people use: Immich's server, Jellyfin, Sonarr, Grafana |
| `backend` | a part with no face of its own: the mail watcher, the bridge |
| `database`, `cache`, `queue`, `storage` | what keeps or carries data |
| `proxy`, `network` | Caddy; gluetun, Forgejo's tailscale |
| `model`, `collector` | Immich's machine learning, Ollama; Alloy, Beszel's agent |
| `runner`, `backup`, `repository` | Forgejo's runners; a vault; restic's repositories |
| `infrastructure`, `certificate`, `package`, `machine` | tofu; `*.twolfe.dev`; an image or tool; a node's profile |

| Today's workflow | A component's `workflow` | |
| --- | --- | --- |
| `docker`, `dotnet-service` | as it is | a component per compose service; `dotnet-service` builds its image first |
| `agents` | as it is | Ollama's servers too, once their models are components of their own (below) |
| `ollama` | the models' | a model is a component, served by one or more Ollama agents |
| `forgejo-runners` | as it is | declarative, so the agent can bootstrap a new node's runner |
| `obsidian` | as it is | a component per vault |
| `restic` | as it is | where backups go: retention, verification, the offsite copy |
| `tofu`, `caddy-certificates`, `image`, `dotnet-tool`, `chezmoi` | as it is | |
| `backup` | `sqlite`, `postgres`, `directory`, `lmdb` | state workflows, to come: one per kind of state, operating the parts that hold it (below) |
| `garage-layout` | a facet | `layout:` on Garage's component |
| `heartbeat`, `gatus-health` | facets | a `heartbeat:` or `check:` on what they watch |
| `caddy-routes` | gone | Caddy renders the published `route` facets |
| `immich-import` | not a component | a one-off operation, and a CLI command |

There are no jobs: what looked like one is a declarative kind (a runner,
a certificate, a repository), a facet of something else, or an operation
rather than a component.

**Decided: a backup belongs to the component that holds the state, one
each, a job of its workflow.** A component's parts are components, so
what used to need a list of typed backups — Sonarr's SQLite and its
config directory, Paperless's SQLite and its media — is two components,
each operated by the state workflow that knows how to back it up:

```yaml
# media/sonarr/compose/component.yaml
name: app
kind: app
workflow: docker
service: sonarr
---
name: database
kind: database
workflow: sqlite
partOf: app
path: config/sonarr.db
---
name: config
kind: storage
workflow: directory
partOf: app
path: config
excludes: [Backups, MediaCover, logs, "logs.db*"]
```

- **Every state workflow backs up with the same shape — prepare, then
  verify — and only its implementation differs.** Preparing stops what must be
  stopped — a `sqlite` file copied while it is written can come back
  corrupt, so its host, found up `partOf`, is stopped — or nothing: a
  `postgres` dumps while it runs. Verifying is what the workflow knows how to
  check after a restore: an integrity check for SQLite, a recent dump for
  Postgres, the named files for a directory. Garage's LMDB store is one
  more workflow, not a special case.
- **`kind: backup` remains for what is only a backup**, as the vaults are.
- **Paths are relative to where the component is deployed.** What it
  ships — config, artifacts — to its release, which mirrors its
  directory in the repository, so in the checkout such a path reads as
  relative to the YAML file itself. What it keeps — state, and so what a
  backup holds — to its service's state directory, `${LAB_DATA}/<service>`
  (the convention already: `~/Docker/sonarr`). An absolute path is for an
  external drive alone, and must fall under one of the component's
  `requiresVolumes`. Jellyfin, the one service still keeping state
  outside its state directory, moves into it first (see "Debt" below).

**Decided: backups are planned by the agent, never scheduled.** No cron
lines, and no offsets: staggering backups by hand-picked minutes overlaps
as they grow, puts every new service at the end, and makes "which runs
last" a search through every schedule. Each component declares only what
it holds; the agent (#6) plans the night from all of them:

- **What runs together**: every part of a host is taken in one stop of
  it, so Sonarr's database and config are one consistent moment, tagged
  together and restored together, and the app is stopped once.
- **How much at once**: concurrency per disk, so two backups never fight
  over one drive, rather than minutes apart in the hope they don't.
- **In what order**: by the service's `lifecycle`, then an optional
  `priority` on the service — production before experimental, and what
  matters most first.
- **When**: nightly, every backup — which is all the lab has ever needed —
  so nothing declares a schedule until something needs another cadence.
  Disruption stays inside the night, and the plan says how long each host
  will be down.

**Decided: a model is a component, served by Ollama agents.** An
Ollama server is an agent like any other (`agents`), and what it serves
is declared beside it, a component per model, linked to the servers it
is pulled on as an agent is to its nodes. The `ollama` workflow operates
the models: it waits for each server, pulls what it serves and points its
role names at it. A role differs by server on purpose — the Studio's
`interactive` is a 30B model, the mini's an 8B one, and the endpoint
degrades from one to the other — so the link carries the roles a model
answers to on each server:

```yaml
# ai/ollama/models/component.yaml
name: qwen3-8b
kind: model
workflow: ollama
model: qwen3:8b
servedBy:
  mini: [background, interactive]
  studio: []                        # pulled there; the Studio's roles are the 30Bs'
---
name: embedding
kind: model
workflow: ollama
model: embeddinggemma:300m
servedBy: { mini: [embedding], studio: [embedding] }
```

- **`identical` goes.** A role one model serves on several servers is the
  same model by construction; a role two models serve on two servers is a
  different model on each, as `interactive` is meant to be.
- **The roles' rules are the catalog's**, checked as a model is added: a
  server answers each role once, and every role a hybrid answers, a
  server answers too, so nothing is "not found" while the Studio sleeps.
- **The store and its drive are the server's.** `OLLAMA_MODELS` is its
  agent's, and the mini's `/Volumes/Data2` its `requiresVolumes`.

**Undecided:** whether `servedBy` names agent components or nodes — the
same until one node runs two Ollamas — and whether the models deploy
with their server, after its restart, or on a trigger of their own.

**Decided: per-node differences are facts of the node, not copies of
the component.** Tested on the hardest case, Alloy — one package on three
nodes, two roles, a socket path only the Studio has, Linux on the Pi —
every difference has a home that is not a per-node entry:

- **What differs by role is two components.** The gateway and the
  forwarders differ in everything placement cares about — responsibility,
  where they run, what they depend on, what they expose, when they move —
  and share only a binary and `node.alloy`. So a **forwarder** runs on
  every node, the mini included, and forwards to the gateway as every
  other node does; and the **gateway**, which needs none of the host's
  visibility that made Alloy a host process, becomes a container in
  Grafana's own stack (`monitoring/grafana`), writing to the stores over
  its `telemetry` network and publishing only `:4417`–`:4419` — so it
  moves with the backend because it is part of it. The mini runs one more
  Alloy, for a forwarder identical everywhere.
- **What differs by platform is a placeholder**: `alloy-{platform}.zip`,
  as a package's names already expand `{version}`.
- **What differs by supervisor is the host's to interpret.** launchd or
  systemd is already the host's choice of unit; so is where an agent's
  log goes — a file the host names and declares to the collector, or the
  journal — so `log` stops being declared.
- **What differs by node is the node's**, declared once (below) rather
  than in every component that needs it: its role, its platform, its
  Docker socket, its drives, its address. `LAB_HOST` and `LAB_ROLE` are
  injected into every agent by the host, never written.
- **What one component needs of another is a reference** — within its
  service, by the rule above, or through #8 across services: the
  forwarders' gateway address is the gateway's published endpoint.

The Beszel agent passes the same test — its socket the same fact, its
`HUB_URL` the hub's placement, its `EXTRA_FILESYSTEMS` the mini's drives —
so the design has **no per-node overrides** until something needs one.

```yaml
# monitoring/alloy/forwarder/component.yaml
name: forwarder
description: Each node's collector, forwarding to the gateway.
kind: collector
workflow: agents
runsOn: every node
package: { github: grafana/alloy, version: 1.20.1, asset: "alloy-{platform}.zip", checksums: SHA256SUMS }
program: "alloy-{platform}"
arguments: [run, forwarder.alloy, "--storage.path={state}", --disable-reporting]
environment:
  LAB_GATEWAY: "{gateway.address}"
  DOCKER_HOST: "{node.docker}"
```

**Decided: nodes are declared, in one file.** `platform/nodes.yaml`, a
document per node — few enough that one file holds them all, and the one
declaration that is not a service's:

```yaml
# platform/nodes.yaml
kind: node
name: studio
role: hybrid
platform: darwin-arm64
address: macstudio.tailf823b8.ts.net
docker: unix:///Users/tomwolfe/.docker/run/docker.sock
---
kind: node
name: mini
role: server
# …
```

They are what placement matches on — `runsOn: every node`, `every
server`, `[mini]` — and what `lab init` (#6) onboards.

**Decided: one placeholder syntax, `{dotted.lower}`.** Everything a
declaration expands — `{platform}`, `{version}`, `{state}`,
`{node.docker}`, `{gateway.address}` — is written one way; `${LAB_ROOT}`
and its kin retire from declarations.

**Decided: any document in any lab file, found by its schema.** A file's
name says nothing: every YAML file git tracks, three directories deep at
most, is read, and the lab's are those naming its schema in their first
line — which is also what gives an editor the model's rules:

```yaml
# yaml-language-server: $schema=../../platform/lab/schema/lab.schema.json
kind: service
name: immich
```

- **Only what git tracks.** The agent (#6) deploys the repository at a
  commit — a tree, not a working directory — so an untracked file is
  never a declaration, and `.gitignore` already leaves out build output;
  the schema line leaves out every other tool's YAML (`compose.yaml`,
  Loki's, Gatus's). No ignore file of the lab's own until something needs
  one; if it does, gitignore's syntax.
- **Every document has a `kind`**, and the schema a branch per kind.
- **Where a document sits says what it belongs to**, whatever the file is
  called: a component to the service whose directory holds it, a service
  in its own directory, nodes in `platform/`. Any object may live in any
  file, but not in any place — which is what keeps `lab.area`,
  `lab.service` and `lab.component` derived.
- **One way in, valid by construction, the rules where they belong.** A
  catalog is read by one reader, which judges the YAML and its shape
  against the schema, with the path of each problem, and adds each
  declaration in an order a valid catalog can be built in — a service
  after the services it depends on, a component after what it is part of
  or depends on. The rules are the domain's, each held by what owns it:
  a service or component is made by its own `Create`, which refuses what
  cannot be one, then added — the catalog refusing a service that breaks
  its rules (unique names and directories, `dependsOn`), and a service a
  component that breaks its own (where it sits, unique names among
  siblings, `partOf`, `dependsOn`). A catalog is valid from empty and
  stays so; the reader's result is that catalog, or every problem.
- **The schema is generated from the domain's types**, so there is one
  truth, and committed, so the relative path in each file works offline
  and in any editor; a test fails when the committed schema is stale.

**Decided: five projects, in the classic layers.**

| Project | Holds | References |
| --- | --- | --- |
| `Wolfe.Lab.Domain` | the model, its value objects and rules | Ritten's `Result`, `Error`, `IDirectory` and `IFile` |
| `Wolfe.Lab.Infrastructure` | the readers — declarations, compose — and the clients: Docker, launchd, systemd, the vault | Domain |
| `Wolfe.Lab.Application` | the Ritten workflows, jobs and steps | Domain, Infrastructure |
| `Wolfe.Lab` | the CLI, a thin host | Application |
| `Wolfe.Lab.Agent` | the reconcile loop, schedules and triggers | Domain, Infrastructure |

The agent references no application code: it orchestrates runs of the
CLI rather than running workflows itself.

**Decided: the agent runs each workflow as a process.** A run of the
pinned CLI, as a child process — through Ritten's own command runner —
so a run is pinned to the version it was planned with, a crash ends the
run and not the agent, and each run's environment carries only its own
secrets (#8's narrowing). Not a container, at least to begin with: an
`agent` component's deploy writes launchd units, a machine applies
chezmoi and a runner registers on the host, and on a Mac a container is a Linux VM
that can reach none of it — the reason the lab has host runners today.
Containers can come later, as hardening, for the kinds that need no host.

**Undecided.**

- **Each facet's schema.** Sketched above, settled as each one moves.
  #8's well-known keys — endpoint, health, route, scrape — are the same
  facets, published, so they share one schema rather than having one
  each.
- **The kinds themselves.** The tables above are a first
  list: whether Loki and Prometheus are `database`s or a kind of their
  own. A part with no face is a `backend`, never a *service*, which is
  the catalog's own word — and `kind: service`, a service's entry.

**Migration is incremental.** The loader reads a fact from its new file
or its old place, never both — the check refuses one declared twice —
and each fact moves once.

**In order.**

1. The design: this item's undecided list, settled and written here.
2. *Slice* retired from the repository in one mechanical sweep — prose
   and code alike — so the new files are written in one vocabulary.
3. The projects: `Wolfe.Lab.Domain`, `Wolfe.Lab.Infrastructure` and
   `Wolfe.Lab.Application` split out of today's one, with the value
   objects and the component's placement moved into the domain.
4. The pilot: the declaration loader, components as logical parts with
   their workflows' documents read polymorphically, and the first facets, `logs` and
   `metrics`, with typed readers in place of the telemetry checks'
   key-walking. The services' own metrics ship on it.
5. Agents, the largest declarations, with placement and the nodes —
   #6's step 4 in the new files — and the Alloy gateway into Grafana's
   stack, leaving a forwarder on every node.
6. The rest, one facet at a time, routes and checks last, where they stop
   being gathered and start being published (#6, #8).

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
- **Services deploy one at a time, but some config belongs to the whole
  repo.** Every service carries a `caddy.caddyfile`, and the front door's
  hook has to gather them; Gatus checks, the collectors' scrape targets
  (monitoring/README.md) and backup schedules are the same shape.
- **A service does not own its own deployment.** Where a component runs is
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
at a commit*, not a service at a time, with the deploy code the CLI
already has:

- **A component declares where it runs**, in its own declaration (#14), so
  "what runs on this node" is a question the repo answers; the per-service
  deploy workflows go, and each service owns everything about itself.
- **What is no longer placed is torn down.** The agent remembers what it
  deployed on its node; a component placed elsewhere, or deleted, is
  stopped, its unit retired, its artifacts removed and its unused
  package versions pruned. Its state under `${LAB_DATA}` is kept — the
  old copy is the safety net until the new node's first backup lands.
- **Cross-service config is published, not gathered**: each component
  publishes its routes, checks and scrape targets to the config plane
  (#8) as it deploys, and caddy, Gatus and Alloy render from what is
  published — reloading only when it changed. No consumer reads another
  service's files.
- **A merge is the desired state, a revert the rollback**, and drift is
  noticed because the agent keeps looking.
- **What changed is decided the way CI decides it.** The agent keeps,
  per component on its node, the last commit it deployed successfully;
  on a new commit it diffs the two and deploys the components whose
  paths changed — `GatePathFilter`'s rule, with the paths a component
  reads outside its directory declared in its declaration (#14) rather than
  in a workflow's `paths:`. So a check that runs in CI means a deploy
  that runs in CD. A failed deploy does not advance its commit, so the
  next pass retries it; an occasional full pass catches drift that did
  not come from the repository.
- **It publishes and installs what components declare** — artifacts to
  `${LAB_ROOT}`, and the pinned packages agents and tools run from
  (platform/lab/README.md) — the same steps the workflows run today.
- **It resolves each component's secrets itself**, which makes it the
  place to narrow what each node can read (#8).
- **It reaches Forgejo directly** — loopback or the tailnet, never
  through caddy — so it never depends on anything it deploys: a broken
  route cannot cut it off from the fix.

**Placement is configuration, not structure.** Today a component is
declared once per node — the Beszel agent three times over, the same
package and program in each — because a push deploy runs on one node's
runner and each runner needs its own entry. Pulled, a component is
declared once, and where it runs is one setting beside it: a list of
nodes, or a rule ("every server", "every node"). What really differs by
node is an override, and most of what differs today goes away without
one — a package's asset is chosen by platform (`assets`, as the tools
manifest does), and a node's facts are variables (`${LAB_HOST}`; the
node a component's dependency is placed on) rather than addresses
written into each entry.

**Moving a service is changing its placement.** Stateless, it is only
that: the new node brings it up and the old node's agent, finding it no
longer placed, takes it down. Stateful — Immich, Paperless, a hub — it
is the standard staged migration: place it on the new node beside the
old, restore its data there (`lab restore`, which the drills already
exercise), cut over, then remove the old placement. A single-instance
service is never torn down before its replacement answers: the old
node's agent waits on the new one's status through the API.

**The agent decides; the operating system keeps daemons alive.** The
agent is a controller and a scheduler, not a supervisor:

- **Daemons stay launchd's and systemd's.** The kernel installs one
  unit, the agent's; the agent reconciles every other daemon into units
  — what the `agents` workflow does today — and checks they are alive.
  Supervising them itself would not remove launchd or systemd (something
  still keeps the agent up), would tie every daemon's lifecycle to the
  agent's upgrades, and on macOS would make the agent the *responsible
  process* for its children's privacy grants: ollama reading a drive
  would need Full Disk Access on the agent, the most privileged process
  in the lab. Starting before login, restart with backoff, limits, logs
  and sleep and wake come with the OS.
- **Schedules are the agent's.** Backups, the offsite copy, verification
  and drills move off Actions cron into the agent, run by **Hangfire**:
  recurring jobs, jobs enqueued from outside (a webhook, a button),
  retries, and no two of a kind at once (`DisableConcurrentExecution`),
  in place of the `MacMini` concurrency group. Its storage is SQLite
  under `${LAB_DATA}` and disposable: schedules are declared by the
  components and re-registered from the repository on every start,
  and history lives in Tempo. *Revisited:* this was once "the agent
  declares, launchd and systemd run", so that 02:30 would not depend on
  the agent being healthy at 02:30. Given up on purpose: in the agent,
  every run is observable — its schedule, next and last run, misfires,
  retries and a trace per run, beside everything else in Grafana —
  where a launchd timer is a log file. Backups also stop depending on
  Forgejo being up, Forgejo's own included.
- **Failures alert through Grafana**, like everything else in the lab: a
  run that failed, a schedule that missed its slot, the agent's own
  health, from its telemetry. healthchecks.io stays for what only an
  observer outside the building can see — that the mini and the Pi are
  alive at all, and that Grafana's own alerting runs (monitoring/README.md) — not a check
  per job: an agent that dies is caught by the node's heartbeat and the
  missing telemetry, not by twenty silent pings.

**It serves an API: ASP.NET Core.** Tailnet-only and authenticated —
this is the process that can deploy anything. A standard host brings
what a hand-built endpoint would not: health checks, OpenTelemetry for
every request, error handling that fails a request rather than the
process, and an OpenAPI document from which clients are generated. It
answers what each node converged, each component's last result, each
schedule's last and next run, and takes a few narrow actions ("reconcile
now", "run this job now"); Forgejo's webhooks arrive on it too. The
Hangfire dashboard mounts on the same host, behind the same
authentication. For development, `lab agent --once` runs a single
reconcile in the foreground.

**The portal dispatches; the agents act.** Each node's agent exposes
its own endpoints, and the portal (#9) is where they meet: one page
across every node, with the button that runs a job on whichever node it
belongs to, through the generated client. The agent carries no UI of its
own beyond the dashboard, so a UI change never restarts the deployer.

**Runs are in-process.** One process per node: fewer moving parts, one
telemetry pipeline, and concurrency controlled by locks in memory rather
than two processes clashing. Each run gets its own DI scope, a timeout
and cancellation, and an exception fails its run, not the agent;
launchd and systemd keep the agent itself up. What today's run-once CLI
gets away with because every run is its own process has to hold for a
process that lives:

- **Nothing a run needs goes in process-wide state.** Commands set their
  own working directory; the process's current directory is never
  changed.
- **Tools are the agent's, one version each.** `EnsureTools` puts a
  pinned tool first on the path — right for an agent, which should run
  one version of `tofu` everywhere — but changing that version takes a
  lock: a job holds its tools for its whole duration, and a new version
  is swapped in only between jobs.

Two things make it safe to hand deployment over: **it is observable
from its first reconcile** (each reconcile a trace and each step a
span, as every run of the lab's CLI is already), and **it reports where the Actions tab does** — a commit status
per node on the commit it converged, so a deploy still goes green or red
in Forgejo.

**Tofu: planned on the pull request, applied after merge, both by the
agent.** Atlantis's shape. Forgejo's pull request webhook reaches the
agent's API, and for a pull request that changes a tofu root it plans
the head commit and posts the plan as a comment and a status; on merge
it applies. Be exact about what
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
to pipeline files living in their services.

**The agent is the lab's own** (#14). A host of its own beside the
run-once CLI — the Generic Host, over the lab's domain — driven by
**triggers** rather than commands: a timer, a webhook, a call on the API.
*Revisited:* this was once "agent mode is a Ritten feature", a second
Ritten host that would have needed a Ritten release before the lab's
agent could exist. Built bespoke instead, until it is clear what works;
whether any of it belongs in Ritten is a question for afterwards.

**`lab init` onboards a node.** `dotnet tool install -g
Wolfe.Lab`, then `lab init`: apply chezmoi for the machine's
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

1. Observability — the agent is not built blind. *Done 2026-10-07*
   (monitoring/README.md).
2. The mini joins the build pool.
3. `lab init`, and the runners out of chezmoi.
4. Placement in the components' declarations — `runs-on` read from the
   component, while the workflows still deploy.
5. The lab's agent: one low-stakes service
   first, then the rest, then caddy's assembled routes.
6. Schedules move from Actions cron into the agent's Hangfire, with
   Grafana rules for failed and missed runs first — backups last, once
   the rest have run quietly for a while.
7. Tofu through the agent — plans on pull requests, applies on merge —
   and CI as one workflow.
8. The trust boundary.

### 8. A config plane — Garage for configuration, the Bitwarden exit for secrets

Two halves because they are the same move —
resolution goes LAN-local while every reference keeps its shape — and
they are separable builds, each landing when its own pressure arrives:
the Garage half with the agent (#6), the first process that lives on
every node and so the first that needs configuration to change under
it; the secrets half once the restore drills (#1) have passed — the
offsite copy exists first and is proven, then the origin moves.

**The config half.** The first cross-service values are already here,
homed nowhere: the runner registration template reads Forgejo's
`ROOT_URL` out of `platform/forgejo/compose/compose.yaml` with a cross-tree `include`, seven tofu roots carry the Garage
endpoint as a `macmini.local` literal, the tailnet suffix is typed into
fourteen files, and the mini's LAN address appears as two different IPs
(`network/caddy/tofu/variables.tf`, `platform/forgejo/README.md`). Every one is retyped
by the Linux move (#13). Hardcoding one service's fact into another is the
thing to refuse; a consumer deriving it from the owning service's files
is only a consumer's guess at a format that isn't its own.

**What it is for: configuration that changes without a restart.** The
shape of Azure App Configuration or SSM Parameter Store: a value changed
in one place reaches every node, pushed or polled, and the running
process picks it up. Redeploying the agent on every node to change a
base URL is the cost this removes. The contract, whatever holds the
values:

- **Layers**: `appsettings.json` (the defaults the tool ships) under the
  store, under `LAB_` environment variables — the per-node override that
  always wins, and the emergency brake.
- **Polling is what makes it correct.** Each agent lists the bucket
  every minute or so — one `ListObjectsV2` carries every key's ETag, so
  no writer has to remember to bump a sentinel — and on any change
  reloads and fires the reload token; `IOptionsMonitor` subscribers see
  the new values.
- **Push is what makes it fast.** Once the event bus lands (Undecided,
  "An event bus"), a `config.changed` message has every agent refresh at
  once; a lost message costs one poll interval. Configuration never waits
  for the bus.
- **Last known good.** Each agent keeps its last validated snapshot on
  disk and starts on it when the store is unreachable. A published value
  that fails validation is refused, the previous one kept, and the
  refusal reported — which is why the provider is the lab's own:
  `IOptionsMonitor` alone only fails when the value is read.
- **A run sees one view.** Each run already gets its own DI scope (#6);
  through `IOptionsSnapshot` it keeps the configuration it started with,
  and the next run gets the new one. Clients move from `IOptions<T>` to
  the monitor or the snapshot as the agent lands; the run-once CLI never
  needed them.
- **The kernel's configuration stays out.** Garage's own endpoint, the
  store's address and the vault's service account stay in
  `appsettings.json`: the agent deploys Garage, so Garage cannot
  configure its own deploy.

**The store: a `config` bucket in Garage, read by a provider the lab
owns.** Garage is LAN-local (every plan already polls it), sits below
every would-be publisher in the bootstrap order, is snapshotted nightly
with everything else, and costs no new service. What it lacks — labels,
versioning — key prefixes and the repository's history cover. Weighed
and not chosen: **Microsoft's App Configuration emulator**, which is the
model Tom knows with the real .NET provider (its Key Vault references
even take a custom resolver), but which Microsoft ships for development
and CI — revisit it if labels or feature flags ever earn their keep;
every consumer sees only `IConfiguration`, so the swap is one provider.
**Consul KV**, a stateful cluster for one feature, whose blocking
queries are the push the bus already gives.

**One owner per key.** Keys are namespaced by the service that owns them
(`media/jellyfin/…`, by area), and two writers publish, never
into another service's namespace:

- **Tofu publishes what only exists after an apply** — endpoints,
  connection strings, addresses — as S3 objects on its apply, so the
  publisher is never staler than the last push. The agent that ran the
  apply sends the nudge, since tofu speaks no AMQP.
- **The agent publishes what a component declares** in its
  declaration (#14), on deploy, and removes it on teardown.

The repository stays the truth: a hand edit in the store is for a value
deliberately not in it, never a shortcut past a merge.

A tofu consumer lists-then-reads (a list tolerates absence where a read
errors) and `count`s the dependent resource on presence, so a root
deploying before its dependency simply omits the wiring and picks it up
on a later plan: partial deployment breaks bootstrap cycles, and the
daily plan's nag makes the eventual consistency visible instead of
silent. Two caveats, named at design time. The failure mode INVERTS —
absent config is a green plan that deploys nothing, so every consumer
carries a tofu `check` making absence at least a named warning (the
`heartbeat/tofu` pattern). And published config is declared truth, not
liveness — proving someone answers is monitoring's job (`gatus/`), not
this one's.

**Operational config the same way: owners publish, consumers render.**
Jellyfin's declaration (#14) says its endpoint and health check; its
deploy publishes them as well-known keys; Gatus renders its
configuration from every published check and reloads, with no change to
Gatus per service — a status page built declaratively. Caddy's routes
and Alloy's scrape targets are the same pattern. Placement comes free:
keys are published where a component deploys and removed when it is
torn down, so the page shows what runs, wherever it runs. The
well-known keys — endpoint, health, route, scrape — have a small typed
schema: a contract between services, not free text. And the inverted
failure mode matters most here, since a check that was never published
is a status page that says nothing is wrong: a failed deploy leaves its
keys in place, only teardown removes them, and each renderer reports
what it rendered.

**Secrets are references in configuration.** A value may be
`secret://<item>/<field>`, resolved through `ISecretProvider` when it is
read and never stored resolved — App Configuration's Key Vault
references, without the vendor's syntax leaking into every file the way
`op://` does today. The Bitwarden move (below) then changes the provider
and nothing else. Rotating a secret does not change its reference, so a
resolved value carries a lifetime, or the rotation sends the nudge too.

**Readable from anywhere; a UI of its own from nowhere.** One read-only
JSON endpoint: every key with its owner, its writer (tofu or agent), the
commit and when it last changed — carried as S3 object metadata — and
secrets only ever as their references. Grafana shows it through the
Infinity data source; whichever portal #9 becomes shows it as a widget
or a page. Each agent exports the version it runs and when it last
reloaded, so a node that has fallen behind is a Grafana panel, not a
guess.

Why Garage and not the vault: configuration and secrets are different
jobs. 1Password is the origin of *secrets*, is a cloud round-trip —
deploys and jobs read it, nothing that runs does — and changing a value
there is a redeploy.

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
  of 2026-09-01) or `bw`/`rbw` with a local cache — rbw's agent holds the
  vault locally, which is the Connect-shaped property: reads cost no
  quota and survive cloud outages. Ritten's `ISecretProvider` is already
  the one door every caller goes through, so the machine half is a second
  provider package behind the neutral `secret://` references (the config
  half) — no caller changes, and no env file changes either;
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

### 9. A lab portal — the docs half, then a UI into the lab

Two halves with different costs, in that order.

**The docs half: runbooks readable from a phone.** The READMEs are the
runbooks — each service's `RUNBOOK.md` — and the
drills (#1) are what prove them. What is missing is a way to *follow*
one without a checked-out repo and a text editor. Cheapest stopgap,
available today: Forgejo renders every README in the browser, so
`code.twolfe.dev` on a phone already works. The real thing is a static
docs site built from the repo's markdown — MkDocs Material or similar,
built by a workflow on push on the containerised runner (#6), served by a static container behind caddy at
`docs.twolfe.dev`. One convention decides the rest: procedures
meant to be followed live in a `runbooks/` tree (or a "Runbook" section
per service README) so the site can put them on a page of their own, apart
from the design prose nobody reads at 2 a.m.

**The UI half: a window into the lab and its agent.** Which one —
Homarr, Homepage, Glance or a custom build — is decided after the agent
(#6), and nothing before then may assume the answer: every surface
(the config plane, the agents' runs, Gatus, the Hangfire dashboard)
exposes JSON or a page of its own, and the portal only arranges them.
More than a status page now: once the agent (#6) takes deployments and schedules off
Actions, this is where the Actions tab's job goes.

- **The lab at a glance**: every service, whether it is up (Gatus),
  what it is running, when it was last backed up and when that backup
  was last proven (restic's snapshots).
- **The agent, per node**: the commit it converged and when, each
  component's last result, each schedule's last and next run — read
  from the agents' APIs — and the narrow actions they offer, "reconcile
  now" and "run this schedule now".
- **History from telemetry**: a reconcile or a backup run opens as its
  trace (Tempo) — steps as spans, with their logs — which is the
  run page Actions gives today.
- **Grafana stays Grafana.** Metrics dashboards and alert rules live
  there (monitoring/README.md); the portal links into it rather than rebuilding it.
- **The lab's history, as a timeline** — something to show: when each
  service arrived, the migrations, the hardware, and the milestones and
  incidents ("first successful restore", "first disk failure"). Related
  to the changelog but not the same list, so it is its own curated file,
  `HISTORY.yaml` at the root: dated events, each with a title, a kind
  (built, migrated, hardware, milestone, incident), a short story and
  links (the pull request, the changelog's day, the component). The CLI
  owns the kinds and the rules — valid dates, unique names, newest first
  — and its check holds the file to them, as it does the telemetry
  names. A workflow tags each new event `history/<date>-<slug>` on the
  lab's last commit that day, so the repository can be browsed as it
  was, and a milestone also becomes a Forgejo release, which makes
  Forgejo's releases page a trophy hall before the portal exists; the
  CLI's `lab/v…` tags stay out of it. The portal renders the file.
  Seeded from the changelog and git when it is built — from the first
  commit on 2026-08-22 — for Tom to prune.

If it is a custom build, a small .NET web app with a front end, not a static page: it calls the
agents across the tailnet and holds nothing of its own, so the agent
deploys it like any other component and a rebuild loses nothing.
Useful without the agent — Gatus, restic and Forgejo's Actions API are
there today — but the agent is what makes it necessary.

### 10. Asking the vaults questions

The Obsidian vaults as something a model answers from, at the desk:
ollama plus a RAG layer over a clone of the vault repositories on
Forgejo (`personal/obsidian/`), not a mount. The corpus is already versioned and
already synced by workflows, so the index job is an ordinary `lab` job
with a clean input.

What it stands on is in place. `lab/embedding` is the same model on
both servers, so an index built on either is searchable from either,
and the index can be a nightly job on the mini like everything else
scheduled. `lab/interactive` is the Studio's model while it is on — a
person is waiting, so a question may use most of the machine for as
long as it takes — and the mini's while it sleeps: worse answers, not
none (`ai/ollama/README.md`, "Roles"). Paperless already runs the same
shape over its documents, a nightly embedding index behind a chat
(`personal/paperless/README.md`, "AI"); worth learning from before building.

Still to decide: the front end — Open WebUI in front of `ai.twolfe.dev`
is the cheapest start, an MCP server for the vault the durable shape
("The AI layer", below) — where the index lives and how it is backed
up (or whether it is simply rebuilt), and whether a bigger interactive
model earns its memory on somebody's workstation.

## Debt no item above retires

### Jellyfin's state directory is where the native app left it

Jellyfin stays on the mini — the drives are there. This is about the
path, on the same machine: `media/jellyfin/compose/compose.yaml` bind-mounts
`~/Library/Application Support/jellyfin` at the same absolute path
inside the container and overrides every `JELLYFIN_*_DIR` to match,
because the database stores absolute paths and item IDs derive from
them. Every other service keeps state under `~/Docker/<service>`; this is
the one mount rooted in a macOS user directory, and the reason its
restore lands under `Users/tomwolfe/Library/…`. The gain is small —
one convention, one less exception in the restore steps — and the cost
is a one-time move: stop, copy to `~/Docker/jellyfin`, rewrite the path
columns in `jellyfin.db` (or accept a full rescan and lost watch state),
change the four variables. Only worth doing after the restore drills
(#1), and only if the exception bothers you more than the rewrite does.

#13 removes the choice. `~/Library/Application Support` does not exist on
Linux, so the path moves when the service migrates and the rewrite becomes
part of that step rather than optional cleanup.

#14 brings it forward: a backup's paths are relative to its service's
state directory, and Jellyfin is the one service outside its own.
Nothing blocks the move: Tom is its only viewer.

### The mini's runner loses Full Disk Access on every Homebrew upgrade

The fix is a signed copy of the runner at a fixed path, so the grant
survives an upgrade that moves Homebrew's binary: a step of the runner
component once the runners' install moves out of chezmoi into the CLI
(#6, `lab init`; registration already has, `platform/forgejo/runners/`). Until then the grant is
re-given by hand after an upgrade. The Studio's runner holds no grants,
so it has no such problem.

### The Studio wakes only by hand

A job for the Studio queues while it sleeps and runs when it next wakes
(`platform/forgejo/README.md`, "The Studio's runner"), which is enough while
nothing is urgent. A Mac cannot be woken from *off* over the network,
and a cold boot behind FileVault stops at the login window before any
LaunchAgent starts; from *sleep*, with "Wake for network access" on, a
Wake-on-LAN packet from the Pi brings it back with the session intact.
So "switched off" means asleep, and a wake step in the workflows that
target it is there to add if a queued deploy ever needs to land sooner.

### The lab works around gaps in Ritten

Ritten 0.21.0 closed every gap the lab went round (2026-10-07): the file
system's times, moves, recursive listings and scratch directories, and
Docker's and git's missing commands. One remains:

- **Deleting a deep directory.** `IDirectory.Delete` cannot remove a path
  longer than .NET will traverse, and chezmoi's HTTP cache files a download
  under one, so `ChezmoiClient` still clears its scratch with `rm -rf`.

### The runners carry labels, not their nodes' names

A deploy knows its node from `LAB_NODE`, which chezmoi exports, so
neither the catalog nor the CLI knows anything of CI. What is left is
naming: every workflow's matrix and `runs-on` still name the runners
`MacMini`, `MacStudio` and `wolfe-pi5`. They should carry the node's own
name, `mini`, `studio`, `pi`. That is one change across every workflow and the runners'
registration (`platform/forgejo/runners`), with branch protection's
required checks changed in the same step; `alloy agent` becomes
`alloy forwarder` with them, its workflow named for what it now runs.

## Undecided

### General file sharing (the third thing Google Drive does)

Backup and vault storage have answers above; sharing files with *other
people* doesn't. It's the only item here that would require exposing
something to the internet, which the lab has never done. Options, unranked:
Tailscale node-sharing if "a few known people" covers it; a public
Nextcloud/Seafile if it doesn't, which deserves its own design pass; or
keep a SaaS for the small subset actually shared and self-host the rest.
Explicitly not blocking the backup work — they're independent.

### Single sign-on

Every service with a login has its own — around nine, Grafana
included — each an admin in the vault. One identity provider
behind caddy (Authentik or Pocket ID the likely candidates), or
Tailscale identity passed through caddy as a header, would make that
one login. Undecided because it is a new thing every login depends on:
the provider down is every UI locked, so it wants a break-glass local
admin kept in each service, and it belongs on a node that is not the
Studio. Not blocking anything; Grafana uses its own login until this is
decided.

### Pi-hole again — and local DNS for `*.lab`

Today nodes address each other by MagicDNS name
(`<host>.tailf823b8.ts.net`) — no IPs in the repo, but the tailnet suffix
recurs and node-to-node traffic rides the tailnet even on the LAN. Local
names under `*.lab`, with Tailscale split DNS pointing the `lab` domain
at the Pi (in `network/tailscale/tofu`), are what replaces that.

It ran on the old Pi until it broke (probably SD card wear; this one
boots from NVMe). The
second Pi is the obvious home for it: DNS wants port 53 and real host
networking, which is exactly what macOS cannot give a container.

The argument is not ad-blocking, though. `network/caddy/README.md` already concedes
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
sessions, and an update that took the lab down (2026-09-02).

**Nearly all of it is the VM, not Docker.** macOS cannot run Linux
containers; every runtime on the mini — Docker Desktop, OrbStack, Colima,
Podman — boots a Linux VM and shares files and ports across the boundary.
The filesystem races, the port interception and the no-host-network problem
all live at that boundary, and **#13 removes the boundary rather than
changing the runtime**, which answers most of this section: Docker Engine
on Linux, natively, as the Pi already runs it. Trying OrbStack or Colima on
the mini is worth an afternoon only if #13 stays unfunded long enough to
hurt.

**Decided: OrbStack, until the Linux node.** Memory is what made it worth
the afternoon. Docker Desktop's VM claims about 12.5 GB of the mini's
16 GB up front, and the mini is short of free pages whenever ollama has a
model loaded (monitoring/grafana/README.md, "Memory"); OrbStack's VM grows and shrinks with its containers
and hands memory back to macOS. It is lighter at idle, its file sharing
is faster, it speaks Docker's API and keeps `host.docker.internal` (which
Alloy's loopback receiver relies on), and it runs without a window —
still an app that starts at login, and free for personal use. It does
not remove the VM: host networking, port 53 and the first-boot sharing
races stay until #13. Colima is the headless, open-source alternative,
but its VM's memory is fixed, which gives up the reason for moving;
Apple's `container` has no Docker API and no compose yet.

The move, the Studio first as the lower-stakes machine, then the mini:
the Beszel agent's `DOCKER_HOST` (Docker Desktop's socket path today),
the runners' headless Docker config (`~/.docker-headless`), the file
shares for `/Volumes`, Alloy's `host.docker.internal`, and the
Brewfile's "deliberately NOT docker" note, which becomes OrbStack's.

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
already written into `media/jellyfin/compose/compose.yaml` for the dead discovery port.
On a **Raspberry Pi running Linux, host networking is real**, so the
blocker simply goes away. Use the Pi 5 over the 4: the recorder database is
write-heavy and benefits from the faster I/O.

**It boots from NVMe, not an SD card**, which matters here: HA's recorder
writes constantly, precisely the workload that wears SD cards out.

**HA Container, not HA OS.** HA OS is an appliance: it cannot be a chezmoi
machine, and its config lives inside Supervisor. HA Container on Raspberry
Pi OS makes the Pi an ordinary managed machine and HA an ordinary compose
service. The cost is losing the add-on ecosystem (Zigbee2MQTT, Node-RED),
which is only a loss if those are wanted.

**On declaring it.** There is no usable OpenTofu provider — the only one
(`Mikescops/homeassistant`) has been unmaintained since January 2021, and
its resources *control* lights and media players rather than declare
configuration, which is Terraform-as-remote-control and the wrong idea
anyway. But that is the wrong axis. HA's native IaC is **YAML in git**:
automations, scripts, scenes, templates and dashboards are all files. Only
integration config entries (`.storage/`, holding OAuth tokens and
discovered devices) are UI-managed — and that is exactly the split this
repo already runs everywhere else, where `network/caddy/compose/Caddyfile` is code and
the Netlify token is vaulted credential state. The decisions are declarable; only
the credentials aren't. That makes HA *better* on this axis than beszel,
whose alert thresholds have no file representation at all.

**The structural cost is paid.** The Pi is a managed node with its own
runner; what is left here is one service run as a compose stack, and the Pi's backup path for
its state.

**The bigger reason is events, not the devices.** Presence from the
companion app, time, device state: HA is where "something happened"
becomes an automation. It is a *participant* on the lab's event bus
(below), not the bus itself: it speaks MQTT natively, which the broker
can carry beside AMQP. It ships an Ollama conversation integration, so
`ai.twolfe.dev` can be its assistant with no new service. The aircon and the TV join the list
above; whether either has a *local* integration depends on the brand, and
is worth checking before assuming it.

**The thermostat is a wired tado X** (bought 2026-09-30), on Matter over
Thread. It works on its own through the tado app, HomeKit and Alexa, and
that is deliberate: it is useful before HA exists, and to whoever lives
here next without a homelab. For HA:

- **Local, over Matter, not tado's cloud.** Since 2026-01-01 tado's API
  allows 100 requests a day without an Auto-Assist subscription, which
  makes HA's `tado` integration the wrong primary path. HA's Matter
  integration, added as a second admin beside Apple Home, controls it on
  the LAN. Schedules and hot water stay cloud-only; if they're wanted in
  HA, [Tado Hijack](https://github.com/banter240/tado_hijack) rations the
  quota.
- **Matter needs the same thing as discovery:** host networking, plus IPv6
  on the LAN. That is the argument above again. The Pi has both, and so
  does the Linux node (#13).
- **The Bridge X stays in**, even with the Apple TV as a Thread border
  router. Without the bridge, tado can't deliver firmware and security
  updates (they need NAT64, which tado provides through its bridge) and
  loses Auto-Assist.

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
and HA is a participant rather than a second bus. A compose stack on the
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
own state is re-derivable and, like `personal/mail/bridge`'s volume, needs no
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
`personal/mail/bridge`'s SMTP (the sender address approved in Amazon's
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

**A UPS.** Not bought. The rack plan places it: a ~650 VA unit on the
floor beside the rack, one battery-backed outlet feeding one of the rear
strips, so the router and switch ride out a blip along with the nodes. The
nightly backups stop and start SQLite and LMDB stores, and a power cut
mid-write is how those corrupt, silently, until a restore fails. Buying it
is the smaller half; the other half is what happens on battery:

- **A clean shutdown, set short.** USB goes to the primary: macOS's Energy
  → UPS setting on the mini today, NUT on the Linux node after #13. The
  point is a clean stop, not runtime.
- **Coming back without a hand.** Power-on after power loss on every node,
  and the whole chain verified once by pulling the plug. That is a drill,
  on the same quarterly rota as the restore drill.
- **Making it visible.** Beszel does not read UPS state. NUT exposes battery
  charge, load and on-battery events, which Gatus and Home Assistant can
  consume. It isn't needed for the shutdown, but it is how "the battery is
  five years old and holds nothing" gets noticed before the day it matters.

The US3000 that came with the node (#13) does not change any of this. It
covers one device, and only UGOS is known to hear it.

**The Linux primary node — bought** (#13). Still to buy: a 16 GB DDR5
stick, more NVMe, and the 1U shelf for the two HDDs.

**Rack power.** Eight UK sockets on two 4-way strips on the rear rail, each
strip on its own wall socket, with seven in use before the node. The node
takes the eighth, retiring the Sabrent gives one back, and the HDD shelf
needs one. That closes only if both drive adapters run from **one** 12 V
brick through a DC splitter, sized for two 3.5" spin-ups (about 2 A each on
12 V, so a 12 V 5 A brick with the adapters' barrel size). Two things that
don't work, or cost too much:

- **The Pi and the Hue Bridge Pro on one USB-C charger.** The Pi 5 wants
  5.1 V at 5 A, which ordinary USB-PD chargers don't offer. At 3 A it caps
  its USB ports, and its NVMe boot drive shares that budget. It keeps its
  own supply.
- **Daisy-chained strips, or a PoE switch bought to free a socket.**

The fallback before either: the Hue Bridge Pro (and the tado Bridge X) can
draw from a USB port on the node, at the cost of their uptime following the
node's reboots. When the UPS lands, its battery-backed outlet feeds one
strip, and what goes on that strip is the next power decision.

**Backup decorrelation.** Backups moved to `/Volumes/Data2`, a separate
physical drive from Data1's media, so one drive failure takes one thing.
It stops short of full separation: both drives share the Sabrent, so a
controller or PSU failure still takes both, as does theft, fire, or an
accidental delete. #13 goes further: data on the node's NVMe, backups on
the HDDs, which share a splitter with each other but not with the data.
Restic to B2 remains the actual second copy.

**Not needed yet:** a Zigbee coordinator. The first Matter-over-Thread
device (the tado X, under "Smart home") brought its own Thread border
router in the tado Bridge X, and the Apple TV is another, so Home Assistant
needs a Matter server and IPv6 on the LAN, not a radio of its own. And the mini's
mDNS flapping between its two interfaces is a config problem (a DHCP
reservation, or disabling the unused interface — see "Router as code"),
not a hardware one.
