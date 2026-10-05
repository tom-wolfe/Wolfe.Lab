# Changelog

All notable changes to the lab are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); entries are dated
rather than versioned — the lab is continuous, not released.

## 2026-10-05

### Added

- **Nodes know their own name.** chezmoi now exports `LAB_NODE` (`mini`, `studio`, `pi`).

- **Nodes, and agents placed on them.** `platform/nodes.yaml` contains the machine definitions.

### Changed

- **Alloy gateway.** The collectors now forward to a dedicated gateway instead of
  one collector being a hybrid config.

- **The agents declare themselves once.** Alloy's forwarder and Beszel's agent run on every node
  from their component, and their logs move to `{lab.root}/logs`.

- **Agents are the catalog's alone.** `ritten.json` no longer declares agents per node, and an
  agents deploy is no longer told its node with `--node`.

## 2026-10-04

### Added

- **Service Catalog.** Services and components describe themselves in YAML instead of `ritten.json`.

- **The catalog's first entries.** All service and component metadata.

### Changed

- **Mail watcher logs.** Logs are now managed through the component metadata instead of the compose.

## 2026-10-03

### Changed

- **A deploy needs its component declared.** Compose, agent and Ollama deploys find the
  component in the catalog, as one of its service's, rather than placing it by its path.

- **The telemetry checks are typed.** Each kind of file is read into what it says
  about the lab's telemetry, compose's through compose itself; problems are `Result` errors.

## 2026-10-02

### Added

- **The Studio has a collector.** Alloy runs on the Studio under launchd.

### Changed

- **The CLI is four projects, in layers.** `Wolfe.Lab.Domain`, `.Infrastructure`
  and `.Application` split out of `Wolfe.Lab`, which stays the tool (ROADMAP #14).

- **Gatus logs only warnings.** At `INFO` it logged every check of every
  endpoint, taking most of the Pi's log volume.

- **Enhanced Mail Watcher telemetry.** The mail watcher now includes ASP.NET traces.

- **"Slice" is retired; a service is a service.** The repository's word for a
  service's directory is gone, prose and code alike (ROADMAP #14).

### Fixed
  
- Mail watcher no longer crashes on inbox 0.

## 2026-10-01

### Added

- **The Pi has a collector, and the mini's is the gateway.** Alloy runs on the 
  Pi as a systemd user unit that forwards over the tailnet to the mini.

- **The last five containers without a health check have one.** Loki, Tempo, 
  Prometheus, qBittorrent and Forgejo's Tailscale sidecar.

- **Paperless reads mail.** Attachments of anything filed into the Proton folder
  `Paperless` are consumed, and the mail is moved to Archive.

- **Agent logs are in Loki.** Alloy ingests log files from launchd/systemd agents.

### Fixed

- **Improved event timezone handling.** The event detector now detects timezones.

## 2026-09-30

### Added

- **Telemetry attribute names are validated.** Checks are run during the pipeline build.

- **Agents publish their log locations.** Agents publish a file that Alloy can use to tail the logs.
- **Component metadata is resolved during jobs.** Including the area and service.

- **The mini's container logs are in Loki.** Alloy reads every
  container's logs through the Docker socket, each stream named for its
  container (`service_name`) and labelled with the node and where in the
  lab the container lives — the labels the deploy puts on it. A
  container that sends its own logs over OTLP is labelled
  `lab.logs: otlp` and left out; the mail watcher is, and its
  hand-written `OTEL_RESOURCE_ATTRIBUTES` goes, since the deploy now
  supplies them. Loki indexes the lab's labels on OTLP logs too, so one
  selector finds both kinds.

- **Every container says where in the lab it lives.** A compose deploy
  writes a `compose.override.yaml` into the release, which compose merges
  by itself, putting `lab.area`, `lab.service` and `lab.component` — the
  component's path — on every service as Docker labels, and as
  `OTEL_RESOURCE_ATTRIBUTES` for an application sending its own
  telemetry (ROADMAP #11 step 3: container logs are collected under
  them). The first deploy of each stack after this recreates its
  containers once, for the new labels.

- **Grafana's AI features answer with the lab's own models.** The LLM
  app (`grafana-llm-app`, pinned and bumped by Renovate) is installed at
  start and provisioned to use `ai.twolfe.dev` as an OpenAI-compatible
  provider, both of its sizes on the `lab/interactive` role. Grafana's
  memory limit rises from 512 MB to 768 MB to make room for its backend.

### Changed

- **The runners run `Wolfe.Lab`.** The pin names the new package,
  `wolfe.lab` 1.0.53; chezmoi's install script gives up `Wolfe.Lab.Build`
  first on a node that still has it — both provide the command `lab`,
  which dotnet will not install twice — and only once the feed answers,
  so a node is never left without one. The CI image installs the new
  package, Renovate watches it, and branch protection requires
  `lab tool / check` alone.

- **The CLI is `platform/lab/tool`, and `Wolfe.Lab`.** `build/` held a
  general CLI in a project called `Wolfe.Lab.Build`, about to become an
  agent too; it is now the `lab` service in `platform/`, its README and
  runbook at `platform/lab/`, the component that builds and publishes it
  at `platform/lab/tool` (#6's agent will be `platform/lab/agent`), and
  the project, package and namespaces are `Wolfe.Lab`. Its workflow is
  `lab tool`. The version is now one more than the last release tag when
  what ships has changed since it — so a move is one change, with no
  record of old paths to keep — and this publishes `Wolfe.Lab 1.0.53`;
  the pin moves to the new package next.
  The tofu state backend, `tofu-state.env`, moved to `platform/garage/`,
  whose it is, and the .NET SDK pin, `global.json`, to the repository's
  root — one pin for the nodes, the CLI and the mail watcher, which had a
  copy of its own.

- **Branch protection accepts the CLI's check under either name**,
  `build` or `lab tool`, so the pull request that moves the CLI to
  `platform/lab/tool` — renaming its workflow to match — can satisfy it.
  Narrowed to `lab tool` once that has merged.

- **The tofu state backend is found in the Garage slice.** Every root's
  shared `tofu-state.env` is looked for as `garage/` — at any level up,
  or inside an area — as restic's files already were, the lookup now
  shared; the file itself moves to `platform/garage/` with the CLI's
  directory (`build/` becomes `platform/lab/tool`), and until then it is
  still found in `build/`.

- **The CLI's version is worked out, not typed.** It is `1.0.<n>`,
  *n* counting the commits that changed what ships, worked out by the
  deploy's first step; a merge that ships nothing keeps the number and publishes
  nothing. Each published CLI tags its commit `lab/v1.0.<n>`. The
  changelog's headings are dates, each day's entries under one, and the
  check no longer asks for a version heading.

- **`platform/` is the last area** (ROADMAP #12): Forgejo, Garage,
  restic, chezmoi, the CI image and Renovate moved under it. Every slice
  is now `<area>/<service>/<component>`. Release names, tofu state keys,
  workflow names and schedules are unchanged, so nothing on a node moves,
  no state migrates and no required status changes.
- **`.chezmoiroot` names `platform/chezmoi/home`**, in the same commit as
  the move. A node's `chezmoi update --init` — what the chezmoi deploy
  runs — pulls and applies from the new root in one pass; a plain
  `chezmoi update` by hand reads the old root before it pulls, applies
  nothing, and catches up on the next run. The chezmoi workflow now also
  fires on `.chezmoiroot`.
- **The CI image builds from the checkout's root one level further up**
  (`context: ../../..`), with its Dockerfile at
  `platform/ci/image/Dockerfile`; Renovate reads
  `platform/renovate/config.json5`.

- **A backup finds the restic slice inside an area.** Every backup
  component reads its repository from the restic slice's env files, found
  by walking up for `restic/`; it now also looks one level down, in each
  area, so `platform/restic` is found from `media/jellyfin/backup` as
  `restic/` is today. The walk stops at the checkout's root rather than
  looking through the node's home directory. Shipped and pinned before
  `platform/` moves (ROADMAP #12), since the move would otherwise leave
  every backup outside `platform/` without a repository.

- **`network/` is the fifth area** (ROADMAP #12): caddy, dns and
  tailscale moved under it, as `network/caddy`, `network/dns` and
  `network/tailscale`. Release names, tofu state keys, workflow names and
  schedules are unchanged, so nothing on a node moves, no state migrates
  and no required status changes; caddy's own routes need no rename, as
  the front door has no snippet of its own.

- **`ai/` is the fourth area** (ROADMAP #12): ollama moved under it, as
  `ai/ollama`. Release names, workflow names and schedules are unchanged,
  so nothing on a node moves and no required status changes; its route is
  now `ai-ollama-server`.

### Removed

- **The CI image's own chezmoi and shellcheck.** A job that runs either
  installs it at the version `.config/lab-tools.json` pins, so the image
  no longer carries copies that could disagree with the nodes.
- **The step that uninstalled `Wolfe.Lab.Build`** from a node's chezmoi
  install script: every node — the mini, the Studio and the Pi — runs
  `Wolfe.Lab` now.
- **ROADMAP #12**, the areas, finished and moved here; the roadmap marks
  #11's instrumented mail watcher shipped, and notes a timeline of the
  lab's history for the portal (#9).

### Fixed

- **Renovate failed silently.** Renovate now correctly reports its exit code.

## 2026-09-29

### Added

- **Alloy** (`monitoring/alloy/`, ROADMAP #11) on the mini, as a host
  agent from its pinned release: OTLP from applications on loopback,
  labelled with the node and forwarded to Loki, Tempo and Prometheus,
  with its own health beside it. Its config is the first agent artifact.

- **Packages and tools** (`build/README.md`): the lab installs what it
  runs from GitHub releases, at pinned versions, checked against each
  release's own checksums. An agent declares its `package` in
  `ritten.json` (`${PACKAGE}` is its directory); a job's tools —
  `tofu`, `restic` — are pinned in `.config/lab-tools.json`, installed
  before the job runs them, and put first on its path. Renovate moves
  both. Nothing is pinned yet: agents and tools move over in their own
  pull requests.
- **Paths may start from `${LAB_ROOT}`, `${LAB_DATA}` or `${PACKAGE}`**
  wherever `ritten.json` takes one.

- **Artifacts** (`build/README.md`, "Artifacts"): a component publishes
  directories of itself to the node, under `${LAB_ROOT}`, and a change to
  them restarts what reads them.
- **`LAB_ROOT` and `LAB_DATA`**, set by chezmoi for the host runners and
  every shell, and expanded in `ritten.json` paths.

- **Grafana** (`monitoring/grafana/`, ROADMAP #11): Loki, Tempo and
  Prometheus behind Grafana at `grafana.twolfe.dev`, on the mini.

### Changed

- **`personal/` is the third area** (ROADMAP #12): Immich, Paperless,
  files, Obsidian and mail moved under it, as `personal/immich`,
  `personal/paperless`, `personal/files`, `personal/obsidian` and
  `personal/mail`. Release names, workflow names and schedules are
  unchanged, so nothing on a node moves and no required status changes;
  their routes are now `personal-<service>-<component>`. The obsidian
  workflow's errors point at the runbook's new path.

- **`media/` is the second area** (ROADMAP #12): Jellyfin, Radarr,
  Sonarr and qBittorrent moved under it, as `media/jellyfin`,
  `media/radarr`, `media/sonarr` and `media/qbittorrent`. Release names,
  workflow names and schedules are unchanged, so nothing on a node moves
  and no required status changes; their routes are now
  `media-<service>-compose`.

- **Waits and retries go through `Microsoft.Extensions.Resilience`.**
  Launchd's unload, Garage's bring-up and ollama's start are one polling
  pipeline each — ask again at an interval, give up at a limit — inside
  their client: a step asks `AwaitReady()` and reports the answer, and
  never sees a limit. Each reports through the same telemetry and is
  testable on a fake clock. Package downloads and the Gatus probe get
  the standard HTTP resilience handler: retries, per-attempt and overall
  timeouts, and a breaker.
- **The CLI reads `appsettings.json`**, shipped beside it in the tool and
  overridden per node by `LAB_` environment variables: every wait's limit
  and interval, and each HTTP client's resilience options, rather than
  numbers in code.

- **Homebrew's `ollama` is gone from the mini and the Studio**: the
  server runs from its pinned package, and its deploy runs that
  package's client. The laptop keeps its own.
- **OrbStack replaces Docker Desktop until the Linux node** (ROADMAP,
  "The container runtime"), for memory first: its VM hands memory back
  to macOS, where Docker Desktop's holds about 12.5 GB of the mini's 16.

- **chezmoi and shellcheck are pinned** in `.config/lab-tools.json`
  (2.72.2 and 0.11.0): the chezmoi check installs both, and a node's
  chezmoi deploy applies with the pinned chezmoi, so a local `lab check`,
  the pull request's and the nodes agree.
- **A package needs no checksum file.** Without `checksums` the asset is
  checked against the SHA-256 GitHub records for it; `bin` names the
  directory a tool's command is nested in.

- **OpenTofu and restic are pinned** in `.config/lab-tools.json` (1.12.6
  and 0.19.1, what the nodes already ran): every tofu, backup and restic
  job installs its version and runs it — on a laptop running `lab check`
  too, so a local check matches the pull request's — and Renovate moves
  them. Homebrew's copies and the Pi's chezmoi download of restic are
  gone.
- **Releases are checked by tests, not by hand.** The CLI's tests build
  every workflow the lab runs, as `lab` does at start, and run the
  installer against the real rsync; the manual smoke test on each pin
  is gone. The CI image gains `rsync` and `bzip2` to run them.
- **Placement is recorded as configuration** (ROADMAP #6): a component
  declared once with where it runs beside it, torn down wherever it is no
  longer placed, so moving a service is changing its placement — with a
  staged migration for one that keeps state.

- **The lab installs its agents.** The Beszel agent (every node, at the
  hub's 0.20.0 — the Pi's had drifted to 0.19.0), Ollama (0.34.4 on the
  mini and the Studio) and Alloy declare a `package`, and the deploy
  installs it; the Brewfile's and the Pi's chezmoi copies of the Beszel
  agent are gone. Renovate moves the Beszel hub and agents as one.
- **A job runs its agent's version.** Installing an agent's package puts
  it first on the job's path, so the `ollama pull` a deploy runs is the
  server's own. Homebrew's `ollama` on the Macs goes once this is pinned.
- **The agent design is revised** (ROADMAP #6): the agent owns
  schedules, in Hangfire; runs in-process behind an ASP.NET Core API;
  and alerts through Grafana, with healthchecks.io kept for what only an
  outside observer can see.

- **Renovate holds majors of the telemetry stack on the dependency dashboard.**

- **`monitoring/` is the first area** (ROADMAP #12): Gatus, the
  heartbeat and Beszel moved under it, as `monitoring/gatus`,
  `monitoring/heartbeat` and `monitoring/beszel`. Release names, tofu
  state keys and workflow names are unchanged, so nothing on a node moves
  and no required status changes; their routes are now
  `monitoring-gatus-compose` and `monitoring-beszel-compose`, and caddy's
  routes deploy fires on a snippet at any depth.

### Fixed

- **Restarting an agent that stops gracefully could leave it unloaded.**
  `launchctl bootout` returns once it has asked the process to stop, not
  once it has; Alloy, flushing its queues, was still loaded when the new
  unit was bootstrapped, which launchd refused ("Input/output error") —
  and the agent stayed down. The deploy now waits for launchd to let go
  (the agent's exit timeout, or launchd's 20 seconds, and a margin)
  before loading the new unit, and says so if it does not.

- **Alloy sent usage statistics to Grafana Labs**, which the rest of the
  stack already doesn't: it runs with `--disable-reporting` now.

- **A deploy's report said every artifact "already matched".** rsync
  itemised its changes only on a rehearsal, so a real publish reported
  none; it itemises every run now. Restarts were unaffected — they read
  the node — but the report was wrong.
- **A deploy run by hand published the CLI's own output.** Ritten's
  `artifacts/` (the run's report) and `temp/` in a component were copied
  into its release, so the next run saw a change and restarted the stack.
  Both are left behind now, like `ritten.json`.

## 2026-09-28

### Changed

- **The lab CLI is pinned once**, in the repository's tool manifest,
  `.config/dotnet-tools.json`. The CI image and chezmoi's install-lab
  script read it, Renovate moves it natively, and `dotnet lab` runs the
  pinned CLI from anywhere in the repository.

- **Both publishes run in the containerised pool, not on the Pi.** The
  CLI's `build` deploy installs the SDK `global.json` names, as its check
  already did, so a Renovate bump moves everything that builds at once.
- **The Pi's SDK is the one `build/global.json` names**, read when
  chezmoi renders its install script, so the version lives in one place.

### Removed

- **The `image` workflow's `deploy`**, and its `registry` settings: the
  CI image is built and pushed by kaniko in its workflow. The check stays,
  and an image may now name a `dockerfile` outside its context's root.

### Fixed

- **The CLI had not published since 0.48.1.** Renovate moved
  `build/global.json` to SDK 10.0.401, and `latestFeature` never rolls
  back a feature band, so the Pi found no SDK it could use.

## 2026-09-25

### Added

- **Renovate** (`renovate/`), every morning on the containerised runner:
  a pull request for every pin that is behind — images, the lab CLI's
  two pins as one, NuGet packages and the .NET SDK, Actions and tofu
  providers — and a dependency dashboard issue for the rest. It runs as
  its own Forgejo user, which `forgejo/tofu` creates with write access to
  Wolfe.Lab; `renovate/tofu` mints the user's token, logged in as it, and
  writes that and a read-only GitHub token as the lab's first Actions
  secrets. `renovate / check`, which validates the config, and
  `renovate tofu / check` are required statuses.
- **Icons.** Added technology icons for the homelab to a new assets/ directory.

- **Paperless's AI features**, against the lab's own models: suggested
  titles, tags, correspondents, types and dates, document chat, and a
  nightly embedding index behind both. It asks for `lab/interactive`
  and `lab/embedding`, so it gets the Studio's model by day and the
  mini's overnight.
- **Model roles on both ollama servers**: `background`, `interactive`
  and `embedding`, the last `embeddinggemma:300m` on both and marked
  identical. Both servers pull it.
- **ollama components are checked on a pull request**, like every
  compose slice.

- **Model roles.** An ollama component can declare `models.roles`: a
  name such as `background` or `embedding`, and the pulled model that
  fills it on that node. A deploy points `lab/<role>` at it and removes
  the alias of a role no longer declared, so a caller asks for the role
  and gets the best model of whichever server answered. Every server
  must declare every role, so a role degrades to the mini's best while
  the Studio sleeps rather than disappearing. A role marked `identical`
  must also name the same model everywhere, which is what an embedding
  needs: vectors from two models are not comparable.
- **An ollama `check` job**, which judges a component's roles on its own
  file and against the slice's other servers.

- **The mail watcher reads travel and stays.** Flights, trains, coaches,
  ferries, hotels, restaurant tables and hire cars, from schema.org markup
  by type and from prose by the model, each with its booking reference. A
  stay given only as dates is an all-day event; an itinerary is one
  invitation per entry, each with its own UID.

### Changed

- **The Studio's `interactive` role is `qwen3:30b-a3b-instruct-2507`**,
  the non-thinking release of the background model, so Paperless's
  suggestions and chat no longer wait 30–60 seconds on thinking.
  `background` keeps `qwen3:30b-a3b`; the mini is unchanged.

- **The mail watcher asks for `lab/background`** rather than naming two
  models and falling back between them: the role degrades to the mini's
  model by itself, so the watcher's fallback client is gone.

- **The runners run `lab` 0.48.1**: an ollama component can declare
  model roles, every server must declare every role, and the ollama
  workflow has a `check` job.

- **The mail watcher uses the Studio's model when available.**

- **The runners run `lab` 0.45.1**: an ollama deploy waits for the server
  it converged, a rehearsal changes nothing, and an agent environment
  that starts a value with `~` is refused before it reaches a node.

### Fixed

- **Garage's health check failed after any redeploy that did not recreate
  the container.** Its config was a single-file bind mount, which keeps
  the file a deploy replaced; every `garage` command in the container
  then found no config. The config is now a mounted directory, with
  `GARAGE_CONFIG_FILE` pointing into it.

- **A role only one server declared passed the check.** The published
  0.48.0 predates the rule, fixed on main without a version of its own:
  that role would be "not found" every evening the Studio sleeps. Every
  server must now declare every role.

- **An email the model could not be asked about was never scanned.** The
  detector turned a model failure into "no event", and the watcher moved
  its watermark past the email.

## 2026-09-24

Nothing shipped: the registration guards this entry listed were left out
of the merge, and landed in 0.40.0.

### Added

- **ollama on the Studio**, above the mini behind `ai.twolfe.dev`:
  `ollama/studio/` deploys it on the Studio's runner, holding every model
  the mini does plus `qwen3:30b-a3b`, the background model. The route asks
  the Studio first and falls through to the mini while it sleeps; servers
  may reach the Studio on 11434 and nothing else; Gatus watches it without
  alerting.

- **The `agents` workflow.** Host processes declared per node, checked on
  the pull request and deployed to each node by its own runner with
  `lab deploy --node <name>` — the shape the node services leaving chezmoi
  move into.

- **Agents deploy to Linux.** `lab deploy` renders a component's agents
  to systemd user units on Linux, as it does to launchd agents on a Mac,
  choosing by the operating system.

- **The Studio has a host runner**, `MacStudio:host`: the mini's launchd
  agent and converge script, capacity 3, and no privacy grants.

- **`lab` is on every runner, pinned.** chezmoi installs CLI 0.35.0 from
  the lab's feed on the mini, the Pi and the Studio, and the CI image
  bakes the same version in as its last layer. Nothing calls it yet.

### Changed

- **The Beszel agent is a lab component**, `beszel/agent/`, the first node
  service out of chezmoi. Each node's agent is declared in its
  `ritten.json` and deployed by its own runner.

- **The runners run `lab` 0.42.0**, so every node can deploy an `agents`
  component.

- **An agent deploy with nothing declared fails** rather than succeeding
  at nothing, so a mistyped node is noticed.

- **The runners run `lab` 0.40.1**, up from 0.35.0: the systemd
  supervisor, the registration guards and Ritten 0.20.0's atomic writes
  reach the nodes and the CI image. 0.40.0 is skipped — its dry runs of an
  agent deploy failed to assemble.

- **Ritten 0.20.0.** Files are written through its `WriteAllText`.

- **A job Forgejo cannot place waits a week, not a day**
  (`ABANDONED_JOB_TIMEOUT`), so a push made while the Studio sleeps runs
  when it wakes.

- **Every workflow runs the pinned `lab`.** A runner executes the
  published, reviewed CLI rather than compiling the checkout, and the
  checks no longer set up .NET to build it — only the CLI's own check and
  the mail watcher's, which compile source, still do. The one exception is
  the CLI's own deploy, which compiles the merged source: its input is the
  source, and on a cold start it is what first fills the feed.
- **The runbooks call `lab`** too; building the checkout is for a CLI
  change in progress (`build/RUNBOOK.md`).
- **The lab's NuGet feed is a source on every lab machine.** chezmoi
  writes the user-level `NuGet.Config` on the MacBook, the Studio, the mini
  and the Pi, so `dotnet tool install -g Wolfe.Lab.Build` needs no
  `--add-source`. Only `Wolfe.Lab.*` is mapped to it: the feed is
  tailnet-only, and every other package still comes from nuget.org alone,
  so a restore off the tailnet does not fail on it.

- **The containerised runner pulls its image from Forgejo's registry**
  (`code.twolfe.dev/tom-wolfe/ci`) before every job, rather than using the
  one the node built — so any node's runner runs the same image.
- **The Pi's host runner sets `DOTNET_ROOT`**, so a global .NET tool
  finds the runtime in `~/.dotnet`.

- **The CLI's version is the lab's.** `build/Directory.Build.props` reads
  it from the newest heading here, so a pinned `lab` names the release
  whose entry says what it contains, and a change to what the CLI ships
  comes with a heading of its own. The first one published is this one.
- **Forgejo's package store is out of the backup.** The CI image's
  layers and the CLI's packages are build outputs of a repository that is
  backed up, and one image is over 400 MB that changes on every rebuild.
  A restore deletes the stale package rows and republishes
  (`forgejo/RUNBOOK.md` "Restore").

### Removed

- chezmoi's Beszel env template and the Pi's Beszel units, and
  `restart_service` on the Homebrew formula.

### Fixed

- **The Studio's ollama never started.** `OLLAMA_MODELS` was
  `~/.ollama/models`, and an environment value reaches the process as
  written: ollama tried to create a directory called `~` at the root of
  a read-only volume.

- **An ollama deploy could fail right after converging.** The supervisor
  returns once the server is launched, not once it answers, and the next
  step asked it for its models. A deploy now waits for it, up to 30s.
- **An ollama rehearsal changed the node.** It created the model store,
  before the approval gate at that; it only says so now. A rehearsal on a
  node with no server yet reports every model as one it would pull,
  rather than failing to ask.

- **The mini ran two Beszel agents.** Homebrew labels a service
  `sh.brew.<name>` now and `homebrew.mxcl.<name>` before that.

- **Moving the `lab` pin failed on every node.** The install script still
  passed `--add-source` for the lab's feed, which `dotnet tool` refuses
  once a source mapping is configured.

- **An agent deploy could not be rehearsed.** Fixed the agent DI registration.

- **`lab register` refuses the two mistakes it used to pass to Forgejo**:
  running where there is no forgejo container, and a vault secret that is
  not 40 characters — an empty one, stored because the command that minted
  it failed quietly.

- **A cold start would have stopped at `lab`.** Its install script was
  `run_onchange_` and failed without a feed — and the feed is on the
  Forgejo a fresh server has not brought up yet. It now runs on every
  apply, does nothing when the pin is installed, and warns rather than
  fails while the feed is unreachable, so the next apply picks it up.

- **The CLI's deploy never built it.** Ritten's pack step does not build,
  and the job went straight from the version checks to the pack; it only
  rehearsed cleanly on a machine that already had a Release build. It now
  restores, builds and tests first, as Ritten's own deploy does.

## 2026-09-23

### Added

- **The CLI ships as a package.** `build/` is a `dotnet-tool` component
  on Ritten 0.19.0's `Ritten.NuGet`: the merge deploys `Wolfe.Lab.Build`
  (`lab`) to Forgejo's NuGet feed.
- **The CI image is pushed** to Forgejo's container registry as
  `code.twolfe.dev/tom-wolfe/ci`. An image component names a `registry`
  to push; one without keeps its images on the node.

- **`tag:hybrid`, the Studio's own tag.** A workstation that also serves
  while it is on.
- **The Studio has a Beszel agent**, with its status alert off: off is
  its normal state.

- **`paperless/`** — Paperless-ngx as a slice: the document archive at
  `paperless.twolfe.dev`, SQLite and a Valkey broker, the database on
  the internal disk and the documents on Data2, both in restic nightly
  with the restore drilled after. The secret key and the first admin
  login come from the vault at deploy.

### Changed

- **`build.yaml` is a `lab` check**, gated on what the pull request
  touched like every other, rather than a full build and test on every
  pull request.
- **The CI image is checked and deployed like every component.** Its
  `build` job is `deploy` (build, gate, push), and a new required
  `check` fails a context with no Dockerfile or a pushed tag that names
  no registry.

- **The `macstudio` profile is checked** by `lab check` like every
  other, and has a node name (`MacStudio`) in the inventory.
- **chezmoi never applies a working copy.** The `sourceDir` override is
  gone: every machine applies its own clone, so a runner's `chezmoi
  update` can never pull a half-finished branch. To try an unmerged
  change, `chezmoi apply --source` the working copy (RUNBOOK.md).

- **`scripts/` is gone.** Every job runs from the CLI: `lab register-runner` on the forgejo workflow and `lab init-layout` on the garage workflow replace the one-off scripts, each with a dispatch workflow; `alert.sh` is the runtime's failure sink and `secrets.sh` is Ritten's provider, with runbooks using `op run` directly; `setup.sh` is the bring-up order as `lab` calls; the finished radarr fold and the repository import are deleted.
- **The Gatus probe runs from the CLI.** `gatus/` has its own workflow: the deploy, plus `lab health` reading `health.url` from `ritten.json`.
- **The heartbeat is its own slice.** `heartbeat/` holds the ping (`lab ping`) and the check's tofu root, moved from `chezmoi/tofu`; `scripts/heartbeat.sh` is gone.
- **The chezmoi check and update run from the CLI.** shellcheck and chezmoi are in the CI image.
- **Certificate renewal now runs from the CLI.**
- **Tofu stacks are deployed from the CLI.**
  

## 2026-09-22

### Added

- **The `macstudio` profile.** The Mac Studio is a workstation like the
  laptop and currently mirrors the `macbook`. It will be expanded later.

### Changed

- **Personal git identity is `tom@twolfe.dev`** on every profile but the
  work laptop.

### Fixed

- **A fresh Mac died before its Brewfile ran.** chezmoi runs a phase's
  scripts in alphabetical order of target name, so the dotnet-tools and
  node scripts ran before the one that installs the SDK and nvm. The
  package script is now `00-install-packages.sh` and goes first.

## 2026-09-20

### Added

- **`ollama/`** — the lab's model endpoint at `ai.twolfe.dev`, and the
  first slice whose stack is a host process. A container on macOS gets 
  no access to the Apple GPU, so a containerised model runs on CPU and is uselessly slow.
- **The Pi has the .NET SDK**, pinned by a chezmoi script and added to the
  job PATH in the runner's config — so the CLI runs on either node.
- **Models are declared.** `ollama/ritten.json` names them and the deploy
  pulls what is missing; a model must name its tag.
- **`mail/`** — Proton Bridge as a slice, so the lab can read and send
  mail at all.
- **The mail watcher.** `mail/watcher/`, its own solution built into an
  image by `lab deploy`. It holds IMAP IDLE open and answers an event it
  finds with a threaded invitation, skipping what Proton already handles
  and reading structured data before asking a model.

### Changed

- **One name per service.** A single `*.twolfe.dev` record replaces the
  two wildcards that sat a label deeper, and every service answers to
  exactly one name at the mini's Tailscale address.
- **One Gatus check per service.** The `lab` and `front door` groups 
  have been merged.
- **Supervised agents are declared, not scripted.** A slice names the
  agents it wants running in its `ritten.json` — the program, its
  arguments, environment and log — and `lab deploy` renders the
  platform's unit and makes the supervisor match it.
- **Every slice deploys with `lab deploy`.** `scripts/deploy.sh` is gone;
  the volumes it took as arguments are declared in each slice's
  `ritten.json`, and caddy's route-gathering hook is two steps in a
  workflow of its own.
- **Job names read from the slice they run in.** The slice-level check is
  `restore-drill` — `verify` said nothing in `forgejo/` — and restic
  keeps `verify` for its repositories. One intent, one verb: `converge`
  is `deploy`.
- **`sync` is `Work`, not `Deploy`.** It deploys nothing.

### Fixed

- **ollama could not start as a launchd agent.** Reading the model store
  on Data2 is gated behind a macOS privacy prompt an agent cannot show;
  it bound its port and blocked. Approving it once at the desk is a
  bootstrap step now.
- **The front door could not reach ollama**, which answers 403 to a Host
  or Origin it does not recognise. The route rewrites both.
- **A rehearsed `compose up` no longer claims it converged.**

## 2026-09-18

### Added

- **`files/`.** Backup of personal files on Data2.
- **The CLI backs up.** `lab backup` is the backup pipeline — stop,
  snapshot, start, with the guards — for any slice whose `ritten.json`
  carries a `backup` section. `files` is the first; the shell matrix
  follows it slice by slice.
- **The CLI ships offsite and verifies.** `lab offsite` (copy to B2,
  retention on both repositories, the heartbeat) and `lab verify` (both
  checks, the offsite data sample) on a `restic` workflow, with the
  policy and the sample in `restic/ritten.json`. The two restic scripts
  are gone.
- **`lab restore`.** On every slice with a backup. Stops, restores, and
  starts the slice.
- **`lab verify` on every slice with a backup.** The slice's
  `backup.verify` restores the verify paths from the latest snapshot 
  into a scratch directory and are asserted non-empty, every night after 
  the slice's backup.
- **Every backup is the CLI's, and every slice owns its schedule.** The
  seven compose slices carry a `backup` section in a `ritten.json` on a
  `service` workflow, immich's workflow lists the same job, and each
  slice's `<slice>-backup.yaml` runs its backup nightly with the restore
  drilled straight after. The central `backup.yaml` matrix is gone.

- **Immich.** The photo library, on Data2, with the phone app as the
  place a photo lands after it is taken. The Google Takeout goes in
  through immich-go, run as a throwaway container by `lab import`.
- **Personal media is backed up; films and television are not.** The
  Immich library rides restic and the offsite copy; the media the arr
  stack can re-acquire stays out. Warm snapshots (`stop=false` in a
  slice's `backup.conf`) for a service whose files are write-once and
  whose database is its own dump.
- **The CLI deploys.** `lab deploy` installs a slice and converges its
  stack, the same steps as `scripts/deploy.sh`; immich is the first
  slice on it.
- **The CLI pages on failure.** A job that fails on a runner alerts
  through Pushover from inside the CLI, so its workflow carries no
  `if: failure()` step. `scripts/alert.sh` remains for the workflows
  still on shell.

### Changed

- **The runner's job timeout is 24 hours.** Each workflow's
  `timeout-minutes` is the limit that applies; the runner's own, which
  was the one-hour default, no longer cuts a first pass short.

### Removed

- `scripts/backup.sh`, the `flows/backup/backup.conf` files, and the
  jellyfin `--with-metadata` hand-run option: artwork is excluded, and a
  "Refresh Metadata" re-downloads it.

## 2026-09-17

### Added

- **The lab has a CLI.** `build/` is a .NET solution on Ritten; the
  obsidian workflows are its first callers, one command each.

- **A `runners` group on the status page.** Gatus asks Forgejo for each
  Actions runner's status.

### Changed

- **The Obsidian vaults are git repositories on Forgejo.** The mini syncs
  each vault into `~/Obsidian/Wolfe.<name>` and pushes every change to
  `Obsidian/Wolfe.<name>`.

- **The mini's runner is supervised by launchd, not `brew services`.**
  A chezmoi-owned agent with `KeepAlive`, so it can keep retrying if the 
  runner comes up before Forgejo itself.

### Removed

- Betterdisplay and Boring Notch, because I don't use them.

## 2026-09-14

### Added

- **The Pi backs up like everything else.** A Linux node writes its
  restic snapshots into the lab's repository on the mini over SFTP.
- **`tailscale/tofu`.** The tailnet policy is a file in the repo: one
  tag on the always-on nodes, my devices reach servers, servers reach
  each other, nothing reaches a workstation. Key expiry and MagicDNS are
  declared rather than clicked.

### Changed

- **Secrets are injected on deploy.** Secrets are now injected during the deployment process, rather than being cached by Chezmoi.

- **A machine is its profile.** The chezmoi facets (owner, portable,
  server) are replaced by one value — `macbook`, `work-macbook`,
  `macmini-node`, `pi-node`
- **The work MacBook leaves the tailnet.** Its profile does not list
  Tailscale; the employer's machine no longer has a route into the lab.

## 2026-09-13

### Added

- **Raspberry Pi 5!** The second node in the lab, with all the fun 
  problems you get when scaling from 1->2.
- **Forgejo Actions.** Runs on all server nodes. Managed by chezmoi.
  One host-connected instance for Wolfe.Lab, one isolated container for
  other CI jobs.

- The Pi is now part of the fleet.

### Changed

- **Beszel 0.18.8 → 0.19.0**, hub and agents together.
- **Gatus moved to the Pi.** The first slice deployed by Forgejo Actions.
- **The mini deploys from Forgejo Actions.** One workflow per mini slice 
  and per tofu root. Kestra keeps the tick and everything that is not CD.

### Removed

- **Kestra.** Its last jobs are cron workflows on the mini's runner over
  the same scripts.

## 2026-09-11

### Added

- **`sonarr/` and `radarr/` — the *arr stack as a renamer.** both pointed at 
  the existing library with no indexers and no download client.

### Changed

- **Jellyfin 10.11.11 → 12.0.** First release under the new `major.minor` 
  versioning; Runbook and rollback in `jellyfin/README.md`.

### Removed

- **The nightly `lab.chezmoi/packages-upgrade` flow.** Unattended
  upgrades of everything on a lone server were both unwise, and requiring of 
  user input, causing them to fail every night.

## 2026-09-02

### Added

- **Adds Gatus as a new status page.** It does endpoint checks, which is the 
  last monitoring piece we were missing.
- **Neat names for web routes.** `status.twolfe.dev` and `code.twolfe.dev`.**

### Fixed

- **Docker Desktop updates are manual again.** After the chezmoi update brought
  the whole lab down trying to update Docker.

- **Caddy reads its Caddyfile through the repo mount, not a file bind.**
- **`renew-certs` reloads caddy with `--force`.**
- **Caddy no longer manages certificates itself** (`auto_https
  disable_certs`). It had started its own Let's Encrypt orders for the
  neat names when recreated a minute ahead of the re-issued cert.

## 2026-09-01

### Added

- **A `restic/` root to run the offsite backup.** One repository local, one remote.

### Changed

- Backups now happen directly through Restic.
- Brave browser now gets installed instead of Chrome.

### Fixed

- Kestra now retries Postgres connections for 5 minutes on startup.

## 2026-08-31

### Added

- **A `dns/` root.** With the Proton Mail DNS records imported from Netlify.
- **OpenTofu CD.** `lab.<slice>/plan` runs on every green tick, reporting drift.
  `lab.<slice>/apply` is always a human in the Kestra UI, because the unofficial 
  providers have had real write bugs. The exception: `kestra/tofu` auto-applies 
  — first-party provider, repo-recoverable resources.
- **The poke.** Push-to-main webhook from Forgejo to the tick.

- Forgejo now gets GitHub-style searching.

- **A second front-door wildcard: `*.ts.twolfe.dev` → the mini's
  Tailscale address.** Every lab name now has a tailnet twin that works
  wherever Tailscale is.
- **The beszel agent goes fleet-wide.** The Brewfile entry moves to the
  all-machines section.

### Changed

- **Kestra is trusted with the Docker socket.** Decision recorded in
  kestra/README.md "The trust model": Kestra is the platform, and PR review
  is the gate now.
- **The SSH boilerplate is gone from every flow.** Forced plugin defaults in
  `kestra/application.yaml` define the bridge connection once; no flow can
  point that task type anywhere else.
- The jellyfin/qbittorrent drive guard becomes a sentinel-file check inside
  the shared deploy script (`mount | grep` can't see host mounts from a
  container).
- **Plan and apply are shared pipelines too** (`scripts/plan.sh`,
  `scripts/apply.sh`): `lab-job` falls back to `scripts/<job>.sh <slice>`
  when a slice has no script of its own, so the tofu flows ship no
  per-slice scripts at all.
- **One compose invocation: everything says `docker compose`.** The headless
  config gains the compose plugin symlink (the buildx pattern), so the
  plugin form now resolves in bridge sessions.

- **Forgejo gets its own tailnet identity.**
  A `tailscale/tailscale` sidecar (userspace, sharing the forgejo
  container's network namespace) puts the container on the tailnet with
  its own address, so clone URLs become: `git@git.twolfe.dev:<user>/<repo>.git`.

### Removed

- Vorssaint's settings are no-longer managed. I keep changing them too much and it's annoying. 

### Fixed

- **Boot race: the media-mounting stacks now refuse to deploy without
  their drives.** On reboot, Docker restarts containers before macOS has
  mounted the external drives, so both deploy scripts now carry a mount 
  guard.
- Make 1Password service mode server-only.

## 2026-08-30

### Added

- **Torrenting moves into a container behind gluetun — a `qbittorrent/`
  slice.** The host NordVPN app tunnelled *everything* the mini did (git,
  brew, ACME, Pushover, the healthchecks ping) because macOS Nord has no
  split tunnelling — so the fix inverts it: only the torrent client is
  tunnelled now, with a kill switch by construction, and the host is off
  Nord entirely.
- **`lab.qbittorrent/backup`** — nightly cold tar of the client's state
  at 02:20, ahead of the existing backup train.
- **Tailscale on all three Macs** — the `tailscale-app` cask in the
  all-machines Brewfile section, and a `tailscale/` README recording the
  decisions: standalone app over the tailscaled formula, MagicDNS on, no
  subnet router, no exit node, no tofu root yet.

### Changed

- `nordvpn` is now a personal-machine cask only, and `transmission`
  leaves the server Brewfile (the qbittorrent slice replaces it). Brew
  never uninstalls on its own — the at-desk removal steps are in
  qbittorrent/README.md.

## 2026-08-29

### Added

- **The lab watches its own hardware — a `beszel/` slice.** CPU, memory,
  disk, network and per-container stats, with history and threshold alerts.
- **The free-space check.** `/Volumes/Data1` and `/Volumes/Data2` are monitored
  via `EXTRA_FILESYSTEMS`.
- **`lab.beszel/health`** — a liveness probe for the monitor itself.
- **`lab.beszel/backup`** — nightly cold tar of the hub's SQLite database at 02:35.
- Added a Caddyfile extension to VS Code that offers syntax highlighting. It's old, but seems to work.
- Added the Docker DX (official) VS Vode extension.
- Added Proton Mail to my personal devices.

### Changed

- **Backups moved to `/Volumes/Data2`.** All five backup scripts and their
  mount guards now target the second physical drive. Data1 held 1.5 TB of
  media *and* every backup, so a single drive failure took both; they are
  now decorrelated. Not a second copy — the drives share an enclosure — but
  a cheap improvement while restic/B2 is still on the roadmap.

- **The Brewfile declares; it no longer installs.** It moves out of
  `.chezmoitemplates` and renders to `~/.Brewfile`.
- A new `lab.chezmoi/packages` flow is chained on the tick, and installs what's missing.
- A new `lab.chezmoi/packages-upgrade` flow runs nightly at 04:20, and moves versions forward.
- Chezmoi apply now installs the brewfile every time on non-server devices.

- **The flow naming convention is enforced**, not just documented. A
  `lifecycle.precondition` on `kestra_flow` asserts that each flow's declared
  `namespace`/`id` match what its path requires (`<slice>/flows/<job>/` ->
  `lab.<slice>/<job>`).
- Every task now has a timeout.

### Fixed

- Kestra upgrade script can now be run over ssh.

- **Healthchecks for kestra and garage**, so every container in the lab now 
  has one.

- **Caddy's healthcheck had never once passed** (`FailingStreak: 3933`).
  busybox `wget` resolves `localhost` to `::1` first, and caddy's admin API
  binds `127.0.0.1` only. Caddy was serving fine throughout — nothing in
  this lab reads Docker healthchecks, which is the gap `lab.beszel/health`
  closes.

## 2026-08-28

### Added

- **Task timeouts, where the bound is justifiable.** A hung task is worse
  than a failed one: no logs, no failure, no alert, and with
  `concurrency: QUEUE` another execution stacks up every interval behind it.

- Added a new `amendf` git alias that combines `git amend && git pushf` to 
  amend a commit that's already been pushed.
- **Alerting — every flow failure now reaches a phone.** One flow does it:
  `system/alert-failed` pushes to Pushover. Severity comes from each flow's
  `alert:` label.
- **A dead man's switch.** `lab.chezmoi/update` pings healthchecks.io as its
  final task. The ping stopping is the only signal that leaves the building.
- **`chezmoi/tofu/`** — declaring the tick's healthchecks.io check (schedule, 
  grace, channels)
- `ROADMAP.md`: what's next and why, including the items deliberately
  deferred (self-hosted secrets, k8s) and why.

- Added [BetterDisplay](https://formulae.brew.sh/cask/betterdisplay) to Chezmoi, installed on desktops only.

### Changed

- **The heartbeat is its own flow** (`lab.chezmoi/heartbeat`), chained on the
  tick's SUCCESS instead of being a task on the tick.

- `obsidian-sync` is now `obsidian`.
- **Flows are namespaced per slice.** `lab.<slice>/<job>` replaces the
  single flat `lab` namespace that had the slice baked into the flow id:
- Flows carry `job:` and `alert:` labels. The cross-cutting axis (every
  backup, every deploy) is a label filter.
- The webhook poke URL moved with the tick:
  `…/executions/webhook/lab.chezmoi/update/<key>`.
- The janitor is `lab.kestra/purge`, not a `system` flow: `system` sits
  outside the prefix the alerter watches, and only the alerter needs that
  exemption.

- **SSL maintenance moved out of caddy**: certificates now come from a nightly `renew-certs` Kestra job.
- The caddy container no longer carries any secret: `caddy.env` (NETLIFY_TOKEN) is consumed only by the renew-certs job.

### Removed

- `caddy/Dockerfile`, the xcaddy build, and the libdns patch apparatus — the stock `caddy:2.10` image is enough now that caddy does no ACME. The headless buildx/credential machinery stays (general-purpose; uncached pulls still need the null helper).

### Fixed

- **`lab-job` closes stdin for every job** (`exec bash "$script" </dev/null`).
  A forced-command job is non-interactive by definition, but the stdin
  Kestra's SSH task hands it is a channel nobody writes to or closes — so
  anything that prompts waits forever.
- **Interactive `chezmoi` on the mini.** `onepassword.mode = "service"` means
  every `onepasswordRead` needs `OP_SERVICE_ACCOUNT_TOKEN`, but only the
  tick's job script exported it — so `ssh macmini.local && chezmoi update`,
  the documented recovery path, failed exactly when you'd reach for it.
  `.zprofile` now exports it on servers.

- The kestra secrets table still called the postgres item `Kestra Postgres`.
  0.9.2 fixed the templates to `kestra-postgres` but not the doc.

- The two kestra env templates disagreed on the postgres item after the kebab-case renames (`Kestra Postgres` vs `kestra-postgres`) — the next rotation or fresh bootstrap would have failed on whichever name no longer existed. Both now read `kestra-postgres`.
- `forgejo/tofu` and `kestra/tofu` state encryption is now `enforced = true` (migrations done): tofu refuses unencrypted state instead of silently accepting it, and the leftover migration-era `unencrypted` method declarations are gone.

- Installed BetterDisplay to hopefully try and stop monitors moving around every time I plug them into the dock.

## 2026-08-27

### Added

- **The front door**: new `caddy` slice — Caddy (custom build with the Netlify DNS-01 module) terminating TLS on `:443` with a single `*.lab.twolfe.dev` wildcard certificate (one cert on purpose: per-name certs would publish every internal hostname to Certificate Transparency logs). The wildcard DNS record lives in `caddy/tofu` (netlify provider — a provider, not a slice; public per-service records will live in the owning slice's tofu). Routes are slice-owned: each service contributes a `caddy.caddyfile` picked up by an import glob, same contract as `flows/` and the job bridge. Services now answer at `https://<name>.lab.twolfe.dev` (see ENDPOINTS.md); port publishes stay as the automation path and fallback.
- Shared `lab` Docker network, owned by the caddy slice; forgejo, kestra, jellyfin and garage join it (kestra keeps `default` too — postgres lives there).
- **State encryption**: OpenTofu client-side encryption (PBKDF2 + AES-GCM) on every Garage-backed tofu root — Garage has no SSE, and state holds secrets. New `caddy` root is born enforced; `forgejo` and `kestra` carry an unencrypted fallback until their first post-change state write, then the fallback comes out (see each `encryption.tf`). Passphrase: 1P `tofu-state-passphrase`. The garage root is exempt — its state is local and deliberately disposable.

### Changed

- Forgejo's identity is now `https://forgejo.lab.twolfe.dev/` (ROOT_URL, DOMAIN, SSH_DOMAIN) — links and clone URLs advertise the proxied name; `macmini.local:3000` still works via the published port.
- The secrets-bootstrap scripts (garage, kestra — and caddy's, which never shipped) collapsed into chezmoi `create_` templates under `chezmoi/home/Docker/`: same semantics (1Password is the origin, the env file is a cache, written only when missing), one declarative layer instead of a script writing a file. Verified: chezmoi never evaluates a `create_` template whose target exists, so `op`/internet stay bootstrap-only dependencies — and the update flow now exports the mini's 1P service account so a headless tick can re-materialize a deleted cache. The kestra script never actually generated anything (the vault-is-origin fix predates this); the stale compose comment claiming it did is gone too.

### Fixed

- Chezmoi should now use the 1Password service account for fetching credentials.
- `setup.sh` now converges caddy FIRST (with `--build`) — its compose owns the shared `lab` network, and every other stack's `external: true` reference fails until it exists.
- The wildcard certificate actually issues now: libdns/netlify v1.2.0 types a DNS zone's `domain` as string (via Netlify's stale open-api models), but for domains REGISTERED THROUGH Netlify the live API returns an object there.
- Headless builds work through the job bridge — the same macOS-headless trap as 0.5.x/0.6.0, on the build path instead of pull, needing TWO fixes. (1) `~/.docker-headless/cli-plugins/docker-buildx` is now a chezmoi-managed symlink to Docker Desktop's plugin: without buildx, compose falls back to the legacy builder outright. (2) The headless Docker config now declares `credsStore: "headless"`, a chezmoi-managed null helper in `~/.docker-headless/bin` (on lab-job's PATH, first) that answers every lookup with "not found" so registry access proceeds anonymously. WHY the bare `{}` config wasn't enough: with no credsStore, the Docker CLI's platform *default* on macOS is osxkeychain whenever that binary is on PATH — so any not-yet-cached image (like a build's base image) hit the locked login keychain (`error getting credentials … keychain cannot be accessed`). Existing stacks never noticed because their pinned images were already local.

- Pull mirrors no longer trigger Actions runs: every feature unit (actions, issues, PRs, wiki, packages, projects, releases) is now off on all 33 mirrors — mirrors are read-only copies, GitHub owns their features. Declared in `mirrors.tf`; Hamelin needed the change applied via a minimal API PATCH because the provider's full-object PATCH 500s on repos with wikis (empty `wiki_branch` = branch rename to `""` — now documented in the forgejo README with the workaround).
- Removed `permissions` from `ignore_changes` (computed-only in the current provider; OpenTofu flags it as redundant).

## 2026-08-26

### Added

- Nightly scheduled backups, staggered clear of the tick's quarter-hour columns. All land on the external drive at `/Volumes/Data1/backups/<thing>`; each keeps the last 10, and kestra's `pre-<version>` upgrade dumps are exempt from pruning.
- `backup-garage` 02:50 (cold copy of `meta/` + `data/`), 
- `backup-forgejo` 03:05 (cold copy of `data/`, ~30s downtime, image tag recorded beside each archive), 
- `backup-kestra` 03:20 (live `pg_dump`, no downtime). 
- `backup-jellyfin` 03:35 (cold copy of config + database + library roots + plugins. 
- The cold backups guard against the deploy race: if the stack is restarted mid-tar, the archive is discarded and the run fails loudly rather than keeping a suspect copy. 
- Forgejo's and Jellyfin's backup scripts now live in-repo (`<svc>/flows/backup/script.sh`).

- `setup.sh`: fresh-server bring-up. Imperative bootstrap, since Kestra can't deploy itself into existence.
- `kestra/scripts/upgrade.sh`: manual, guarded kestra upgrade (kestra deliberately has **no** deploy flow, because it can't safely replace its own executor). Takes a Postgres backup first.
- Janitor flow (`kestra/flows/purge/flow.yaml`): nightly purge of >30-day execution history — the 15-minute tick fan-out would otherwise grow postgres forever.
- `kestra/flows/backup/script.sh`: dated `pg_dump` of the kestra DB (husk-proof: dumps to `.partial`, renames on success). Callable by hand or as `kestra/backup` through the bridge — flow-ready for a future scheduled backup; `upgrade.sh` delegates to it with a `pre-<version>` label.

### Changed

- The tick now runs `chezmoi update --init`: the config file is derived state, so regenerate it when its template changes instead of warning on every apply forever. Headless-safe because the config template only uses `promptChoiceOnce`.
- Removed the one-time `moved` blocks from `kestra/tofu/flows.tf` now the first post-restructure apply has migrated the state keys.

- **Vertical slicing**: the repo is now organized by *thing* rather than by *tool*. `compose/<svc>`, `tofu/<svc>` and stray script directories merged into per-thing slices at the repo root.
- `tofu/kestra` + `jobs/` became one directory per job (`<slice>/flows/<job>/flow.yaml` + `script.sh` side by side.
- **Chezmoi declares, Kestra acts**: the `run_onchange` deploy hook is gone. Each service now has a `deploy-<svc>` Kestra flow chaining on `chezmoi-update` SUCCESS.
- The chezmoi tick runs every 15 minutes (was hourly). With no post-merge poke yet, the tick is the only delivery path.
- `lab-job` names are now two-segment `slice/job` paths (e.g. `forgejo/deploy`, `obsidian/main`) resolved to `<slice>/flows/<job>/script.sh` at the repo root. Job names no longer need to be globally unique.
- Garage cluster layout init moved out of chezmoi (`run_once` deleted) into `garage/scripts/init-layout.sh`, invoked by the new `setup.sh`.

### Removed

- `DOCKER_HOST` hack that never fixed anything.

### Fixed

- Chezmoi now `init`s before updating.
- Removed untracked `permissions` attributes causing warnings on the forgejo tofu stack.

- Compose stacks actually converge through the Kestra bridge now. `docker compose` is a CLI plugin resolved through `$DOCKER_CONFIG/cli-plugins` (never PATH), and Docker Desktop on macOS ships its plugins only in `~/.docker/cli-plugins`.

## 2026-08-25

### Added

- Added new `git aliases` alias for outputting existing aliases.
- There is a new `forgejo.scripts/import.sh` script used to import some old repositories. Kept for posterity.

- Lots of git aliases: `git undo` unwinds the last commit, `git main` puts you back on latest main, `git sweep` removes merged branches, and `git catchup` applies the latest changes from `main`.

### Changed

- Forgejo PRs should now default to Squash.
- Forgejo instance is now named `WolfeForge`.

- Git fetch now automatically prunes.

### Removed

- Removed old launchd `ob sync` scripts now that sync is done through Kestra instead.

### Fixed

- `lab-job` now pins `DOCKER_HOST` to Docker Desktop's user-level socket. The headless `DOCKER_CONFIG` has no contexts store, so bridge jobs fell back to the privileged `/var/run/docker.sock` symlink, which isn't guaranteed to exist.

- The Foregejo mirrors had incorrectly configured PATs, so I've recreated them.
- The OpenTofu stack now correctly ignores driftable config like `internal_tracker` and `permissions` because the provider doesn't keep them stable. 

- Node LTS is now installed and aliased as nvm's default by chezmoi. The server script also reinstalls global npm tools into the new version.
- `nvm-run` now warns loudly when it falls back to the default alias instead of hiding it.

## 2026-08-24

### Added

- 1Password Service Account support. 1Password now authenticates using a service account, if the token for one is saved at `~/Docker/1password/service-account-token` (chmod 600). (Service account has also been configured on the server)

- `compose/kestra`: Kestra job scheduler (+ Postgres) on the mini, replacing launchd's scheduling
- `tofu/kestra`: every flow as YAML in the repo, applied declaratively
- SSH job bridge: Kestra reaches the host only through a forced-command key (`restrict,command=lab-job`) that resolves job names to `jobs/*.sh` in the repo checkout
- Flows: `chezmoi-update` (hourly + CI-pokeable webhook), `obsidian-sync-main` and `obsidian-sync-dnd` (one-shot passes every 10 minutes, replacing the launch agents after cutover)
- Headless `DOCKER_CONFIG` (`~/.docker-headless`, no osxkeychain credsStore) so registry pulls work in SSH sessions without the login keychain

### Changed

- The Postgres health check now does `pg_isready` then continues with a `SELECT 1` to test the database is actually reachable. 

- Runtime secrets are now materialized from 1Password, never generated on-machine. (Forgejo and Jellyfin's admin passwords are DB state, not env secrets.)

### Fixed

- The timezone setting to match to the host didn't work, so just set the timezone literally.
- Run Postgres as the host user so it works properly.

- The timezone setting to match to the host didn't work, so just set the timezone literally.

- The bootstrap kestra secrets script had a logic error in it that caused the env file to get written before the SSH key existed, so it was left blank. 

- The 1Password secrets should now use the correct field names: `/password` for passwords, `/credential` for keys.

- The 1Password secrets now reference the correct vault name of `Wolfe.Lab` instead of `Personal`.

- The `TheBoredTeam/boring-notch/boring-notch` is now trusted correctly.

## 2026-08-23

### Added

- chezmoi-managed, machine-aware configuration for all three machines: Brewfiles, dotfiles, macOS defaults, app preferences, and background tasks)
- SSH access to the Mac mini: 1Password SSH agent everywhere, managed `authorized_keys`, commit signing, HTTPS→SSH rewrite for GitHub
- `compose/`: forgejo, jellyfin, and garage stacks deployed by `chezmoi update` on the server (`run_onchange` deploy hook)
- Garage S3-compatible object store (`compose/garage`) with chezmoi-driven bootstrap: secrets generation, cluster layout
- `tofu/bootstrap`: disposable-state project seeding the OpenTofu state store (bucket `tofu-state` + key), credentials vaulted in 1Password
- `tofu/forgejo`: all 34 GitHub repos managed declaratively, with pull mirrors across the Forgejo orgs, with per-repo `mode` switch (mirror/active)
- Wolfe.Lab promoted to Forgejo-primary, push-mirrored to GitHub on every commit
- House secrets pattern: committed `secrets.env` files of `op://` references resolved at spawn by `op run`. This needs to be revisited when Environments are better supported through the CLI.
- `ENDPOINTS.md` service address table; this changelog

### Changed

- Mac mini git auth moved from the 1Password agent to a read-only accountdeploy key (headless-safe); commit signing disabled on the server to work around the UI prompt.
- dotnet tooling switched from brew formula to the `dotnet-sdk` cask (self-registering runtime)

### Fixed

- zsh completion never initialised (`compinit`). Tab completion now works fleet-wide; nschema completion served statically from `site-functions`
- nvm double-initialization; `.zprofile`/`.zshrc` responsibilities untangled
- Launch agent reload: bootout/bootstrap race, third-party agent scoping, wrong chezmoi script phase
- Non-interactive SSH PATH gaps (`/usr/local/bin`) in server scripts

## 2026-08-22

### Added

- Initial repository: Brewfiles and terminal profile
