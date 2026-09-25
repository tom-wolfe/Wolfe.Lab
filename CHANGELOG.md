# Changelog

All notable changes to the lab are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); entries are dated
rather than versioned — the lab is continuous, not released.

## [0.53.0] - 2026-09-25

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

## [0.52.0] - 2026-09-25

### Changed

- **The Studio's `interactive` role is `qwen3:30b-a3b-instruct-2507`**,
  the non-thinking release of the background model, so Paperless's
  suggestions and chat no longer wait 30–60 seconds on thinking.
  `background` keeps `qwen3:30b-a3b`; the mini is unchanged.

## [0.51.0] - 2026-09-25

### Changed

- **The mail watcher asks for `lab/background`** rather than naming two
  models and falling back between them: the role degrades to the mini's
  model by itself, so the watcher's fallback client is gone.

## [0.50.0] - 2026-09-25

### Added

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

## [0.49.0] - 2026-09-25

### Changed

- **The runners run `lab` 0.48.1**: an ollama component can declare
  model roles, every server must declare every role, and the ollama
  workflow has a `check` job.

## [0.48.1] - 2026-09-25

### Fixed

- **A role only one server declared passed the check.** The published
  0.48.0 predates the rule, fixed on main without a version of its own:
  that role would be "not found" every evening the Studio sleeps. Every
  server must now declare every role.

## [0.48.0] - 2026-09-25

### Added

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

## [0.47.0] - 2026-09-25

### Added

- **The mail watcher reads travel and stays.** Flights, trains, coaches,
  ferries, hotels, restaurant tables and hire cars, from schema.org markup
  by type and from prose by the model, each with its booking reference. A
  stay given only as dates is an all-day event; an itinerary is one
  invitation per entry, each with its own UID.

### Changed

- **The mail watcher uses the Studio's model when available.**

### Fixed

- **An email the model could not be asked about was never scanned.** The
  detector turned a model failure into "no event", and the watcher moved
  its watermark past the email.

## [0.46.0] - 2026-09-25

### Changed

- **The runners run `lab` 0.45.1**: an ollama deploy waits for the server
  it converged, a rehearsal changes nothing, and an agent environment
  that starts a value with `~` is refused before it reaches a node.

## [0.45.1] - 2026-09-24

### Fixed

- **The Studio's ollama never started.** `OLLAMA_MODELS` was
  `~/.ollama/models`, and an environment value reaches the process as
  written: ollama tried to create a directory called `~` at the root of
  a read-only volume.

## [0.45.0] - 2026-09-24

### Added

- **ollama on the Studio**, above the mini behind `ai.twolfe.dev`:
  `ollama/studio/` deploys it on the Studio's runner, holding every model
  the mini does plus `qwen3:30b-a3b`, the background model. The route asks
  the Studio first and falls through to the mini while it sleeps; servers
  may reach the Studio on 11434 and nothing else; Gatus watches it without
  alerting.

### Fixed

- **An ollama deploy could fail right after converging.** The supervisor
  returns once the server is launched, not once it answers, and the next
  step asked it for its models. A deploy now waits for it, up to 30s.
- **An ollama rehearsal changed the node.** It created the model store,
  before the approval gate at that; it only says so now. A rehearsal on a
  node with no server yet reports every model as one it would pull,
  rather than failing to ask.

## [0.44.1] - 2026-09-24

### Fixed

- **The mini ran two Beszel agents.** Homebrew labels a service
  `sh.brew.<name>` now and `homebrew.mxcl.<name>` before that.

## [0.44.0] - 2026-09-24

### Changed

- **The Beszel agent is a lab component**, `beszel/agent/`, the first node
  service out of chezmoi. Each node's agent is declared in its
  `ritten.json` and deployed by its own runner.

### Removed

- chezmoi's Beszel env template and the Pi's Beszel units, and
  `restart_service` on the Homebrew formula.

## [0.43.0] - 2026-09-24

### Changed

- **The runners run `lab` 0.42.0**, so every node can deploy an `agents`
  component.

## [0.42.0] - 2026-09-24

### Added

- **The `agents` workflow.** Host processes declared per node, checked on
  the pull request and deployed to each node by its own runner with
  `lab deploy --node <name>` — the shape the node services leaving chezmoi
  move into.

### Changed

- **An agent deploy with nothing declared fails** rather than succeeding
  at nothing, so a mistyped node is noticed.
## [0.41.1] - 2026-09-24

### Fixed

- **Moving the `lab` pin failed on every node.** The install script still
  passed `--add-source` for the lab's feed, which `dotnet tool` refuses
  once a source mapping is configured.

## [0.41.0] - 2026-09-24

### Changed

- **The runners run `lab` 0.40.1**, up from 0.35.0: the systemd
  supervisor, the registration guards and Ritten 0.20.0's atomic writes
  reach the nodes and the CI image. 0.40.0 is skipped — its dry runs of an
  agent deploy failed to assemble.

## [0.40.1] - 2026-09-24

### Fixed

- **An agent deploy could not be rehearsed.** Fixed the agent DI registration.

## [0.40.0] - 2026-09-24

### Added

- **Agents deploy to Linux.** `lab deploy` renders a component's agents
  to systemd user units on Linux, as it does to launchd agents on a Mac,
  choosing by the operating system.

### Changed

- **Ritten 0.20.0.** Files are written through its `WriteAllText`.

### Fixed

- **`lab register` refuses the two mistakes it used to pass to Forgejo**:
  running where there is no forgejo container, and a vault secret that is
  not 40 characters — an empty one, stored because the command that minted
  it failed quietly.

## [0.39.0] - 2026-09-24

Nothing shipped: the registration guards this entry listed were left out
of the merge, and landed in 0.40.0.

## [0.38.0] - 2026-09-24

### Added

- **The Studio has a host runner**, `MacStudio:host`: the mini's launchd
  agent and converge script, capacity 3, and no privacy grants.

### Changed

- **A job Forgejo cannot place waits a week, not a day**
  (`ABANDONED_JOB_TIMEOUT`), so a push made while the Studio sleeps runs
  when it wakes.

## [0.37.0] - 2026-09-24

### Changed

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

### Fixed

- **A cold start would have stopped at `lab`.** Its install script was
  `run_onchange_` and failed without a feed — and the feed is on the
  Forgejo a fresh server has not brought up yet. It now runs on every
  apply, does nothing when the pin is installed, and warns rather than
  fails while the feed is unreachable, so the next apply picks it up.

## [0.36.0] - 2026-09-24

### Added

- **`lab` is on every runner, pinned.** chezmoi installs CLI 0.35.0 from
  the lab's feed on the mini, the Pi and the Studio, and the CI image
  bakes the same version in as its last layer. Nothing calls it yet.

### Changed

- **The containerised runner pulls its image from Forgejo's registry**
  (`code.twolfe.dev/tom-wolfe/ci`) before every job, rather than using the
  one the node built — so any node's runner runs the same image.
- **The Pi's host runner sets `DOTNET_ROOT`**, so a global .NET tool
  finds the runtime in `~/.dotnet`.

## [0.35.0] - 2026-09-24

### Changed

- **The CLI's version is the lab's.** `build/Directory.Build.props` reads
  it from the newest heading here, so a pinned `lab` names the release
  whose entry says what it contains, and a change to what the CLI ships
  comes with a heading of its own. The first one published is this one.
- **Forgejo's package store is out of the backup.** The CI image's
  layers and the CLI's packages are build outputs of a repository that is
  backed up, and one image is over 400 MB that changes on every rebuild.
  A restore deletes the stale package rows and republishes
  (`forgejo/RUNBOOK.md` "Restore").

### Fixed

- **The CLI's deploy never built it.** Ritten's pack step does not build,
  and the job went straight from the version checks to the pack; it only
  rehearsed cleanly on a machine that already had a Release build. It now
  restores, builds and tests first, as Ritten's own deploy does.

## [0.34.0] - 2026-09-23

### Added

- **The CLI ships as a package.** `build/` is a `dotnet-tool` component
  on Ritten 0.19.0's `Ritten.NuGet`: the merge deploys `Wolfe.Lab.Build`
  (`lab`) to Forgejo's NuGet feed.
- **The CI image is pushed** to Forgejo's container registry as
  `code.twolfe.dev/tom-wolfe/ci`. An image component names a `registry`
  to push; one without keeps its images on the node.

### Changed

- **`build.yaml` is a `lab` check**, gated on what the pull request
  touched like every other, rather than a full build and test on every
  pull request.
- **The CI image is checked and deployed like every component.** Its
  `build` job is `deploy` (build, gate, push), and a new required
  `check` fails a context with no Dockerfile or a pushed tag that names
  no registry.

## [0.33.0] - 2026-09-23

### Added

- **`tag:hybrid`, the Studio's own tag.** A workstation that also serves
  while it is on.
- **The Studio has a Beszel agent**, with its status alert off: off is
  its normal state.

### Changed

- **The `macstudio` profile is checked** by `lab check` like every
  other, and has a node name (`MacStudio`) in the inventory.
- **chezmoi never applies a working copy.** The `sourceDir` override is
  gone: every machine applies its own clone, so a runner's `chezmoi
  update` can never pull a half-finished branch. To try an unmerged
  change, `chezmoi apply --source` the working copy (RUNBOOK.md).

## [0.32.0] - 2026-09-23

### Added

- **`paperless/`** — Paperless-ngx as a slice: the document archive at
  `paperless.twolfe.dev`, SQLite and a Valkey broker, the database on
  the internal disk and the documents on Data2, both in restic nightly
  with the restore drilled after. The secret key and the first admin
  login come from the vault at deploy.

## [0.31.0] - 2026-09-23

### Changed

- **`scripts/` is gone.** Every job runs from the CLI: `lab register-runner` on the forgejo workflow and `lab init-layout` on the garage workflow replace the one-off scripts, each with a dispatch workflow; `alert.sh` is the runtime's failure sink and `secrets.sh` is Ritten's provider, with runbooks using `op run` directly; `setup.sh` is the bring-up order as `lab` calls; the finished radarr fold and the repository import are deleted.
- **The Gatus probe runs from the CLI.** `gatus/` has its own workflow: the deploy, plus `lab health` reading `health.url` from `ritten.json`.
- **The heartbeat is its own slice.** `heartbeat/` holds the ping (`lab ping`) and the check's tofu root, moved from `chezmoi/tofu`; `scripts/heartbeat.sh` is gone.
- **The chezmoi check and update run from the CLI.** shellcheck and chezmoi are in the CI image.
- **Certificate renewal now runs from the CLI.**
- **Tofu stacks are deployed from the CLI.**
  
## [0.30.0] - 2026-09-22

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

## [0.29.0] - 2026-09-20

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

## [0.28.0] - 2026-09-18

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

### Changed

- **The runner's job timeout is 24 hours.** Each workflow's
  `timeout-minutes` is the limit that applies; the runner's own, which
  was the one-hour default, no longer cuts a first pass short.

### Removed

- `scripts/backup.sh`, the `flows/backup/backup.conf` files, and the
  jellyfin `--with-metadata` hand-run option: artwork is excluded, and a
  "Refresh Metadata" re-downloads it.

## [0.27.0] - 2026-09-18

### Added

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

## [0.26.0] - 2026-09-17

### Changed

- **The Obsidian vaults are git repositories on Forgejo.** The mini syncs
  each vault into `~/Obsidian/Wolfe.<name>` and pushes every change to
  `Obsidian/Wolfe.<name>`.

### Added

- **The lab has a CLI.** `build/` is a .NET solution on Ritten; the
  obsidian workflows are its first callers, one command each.

## [0.25.0] - 2026-09-17

### Changed

- **The mini's runner is supervised by launchd, not `brew services`.**
  A chezmoi-owned agent with `KeepAlive`, so it can keep retrying if the 
  runner comes up before Forgejo itself.

### Added

- **A `runners` group on the status page.** Gatus asks Forgejo for each
  Actions runner's status.

### Removed

- Betterdisplay and Boring Notch, because I don't use them.

## [0.24.0] - 2026-09-14

### Changed

- **Secrets are injected on deploy.** Secrets are now injected during the deployment process, rather than being cached by Chezmoi.

## [0.23.0] - 2026-09-14

### Changed

- **A machine is its profile.** The chezmoi facets (owner, portable,
  server) are replaced by one value — `macbook`, `work-macbook`,
  `macmini-node`, `pi-node`
- **The work MacBook leaves the tailnet.** Its profile does not list
  Tailscale; the employer's machine no longer has a route into the lab.

## [0.22.0] - 2026-09-14

### Added

- **The Pi backs up like everything else.** A Linux node writes its
  restic snapshots into the lab's repository on the mini over SFTP.
- **`tailscale/tofu`.** The tailnet policy is a file in the repo: one
  tag on the always-on nodes, my devices reach servers, servers reach
  each other, nothing reaches a workstation. Key expiry and MagicDNS are
  declared rather than clicked.

## [0.21.0] - 2026-09-13

### Added

- **Raspberry Pi 5!** The second node in the lab, with all the fun 
  problems you get when scaling from 1->2.
- **Forgejo Actions.** Runs on all server nodes. Managed by chezmoi.
  One host-connected instance for Wolfe.Lab, one isolated container for
  other CI jobs.

### Changed

- **Beszel 0.18.8 → 0.19.0**, hub and agents together.
- **Gatus moved to the Pi.** The first slice deployed by Forgejo Actions.
- **The mini deploys from Forgejo Actions.** One workflow per mini slice 
  and per tofu root. Kestra keeps the tick and everything that is not CD.

### Removed

- **Kestra.** Its last jobs are cron workflows on the mini's runner over
  the same scripts.

## [0.20.0] - 2026-09-13

### Added

- The Pi is now part of the fleet.

## [0.19.0] - 2026-09-11

### Added

- **`sonarr/` and `radarr/` — the *arr stack as a renamer.** both pointed at 
  the existing library with no indexers and no download client.

### Changed

- **Jellyfin 10.11.11 → 12.0.** First release under the new `major.minor` 
  versioning; Runbook and rollback in `jellyfin/README.md`.

## [0.18.2] - 2026-09-11

### Removed

- **The nightly `lab.chezmoi/packages-upgrade` flow.** Unattended
  upgrades of everything on a lone server were both unwise, and requiring of 
  user input, causing them to fail every night.

## [0.18.1] - 2026-09-02

### Fixed

- **Docker Desktop updates are manual again.** After the chezmoi update brought
  the whole lab down trying to update Docker.

## [0.18.0] - 2026-09-02

### Added

- **Adds Gatus as a new status page.** It does endpoint checks, which is the 
  last monitoring piece we were missing.
- **Neat names for web routes.** `status.twolfe.dev` and `code.twolfe.dev`.**

### Fixed

- **Caddy reads its Caddyfile through the repo mount, not a file bind.**
- **`renew-certs` reloads caddy with `--force`.**
- **Caddy no longer manages certificates itself** (`auto_https
  disable_certs`). It had started its own Let's Encrypt orders for the
  neat names when recreated a minute ahead of the re-issued cert.

## [0.16.0] - 2026-09-01

### Added

- **A `restic/` root to run the offsite backup.** One repository local, one remote.

### Changed

- Backups now happen directly through Restic.
- Brave browser now gets installed instead of Chrome.

### Fixed

- Kestra now retries Postgres connections for 5 minutes on startup.

## [0.15.0] - 2026-08-31

### Added

- **A `dns/` root.** With the Proton Mail DNS records imported from Netlify.
- **OpenTofu CD.** `lab.<slice>/plan` runs on every green tick, reporting drift.
  `lab.<slice>/apply` is always a human in the Kestra UI, because the unofficial 
  providers have had real write bugs. The exception: `kestra/tofu` auto-applies 
  — first-party provider, repo-recoverable resources.
- **The poke.** Push-to-main webhook from Forgejo to the tick.

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

## [0.14.0] - 2026-08-31

### Added

- Forgejo now gets GitHub-style searching.

### Changed

- **Forgejo gets its own tailnet identity.**
  A `tailscale/tailscale` sidecar (userspace, sharing the forgejo
  container's network namespace) puts the container on the tailnet with
  its own address, so clone URLs become: `git@git.twolfe.dev:<user>/<repo>.git`.

### Removed

- Vorssaint's settings are no-longer managed. I keep changing them too much and it's annoying. 

## [0.13.0] - 2026-08-31

### Added

- **A second front-door wildcard: `*.ts.twolfe.dev` → the mini's
  Tailscale address.** Every lab name now has a tailnet twin that works
  wherever Tailscale is.
- **The beszel agent goes fleet-wide.** The Brewfile entry moves to the
  all-machines section.

### Fixed

- **Boot race: the media-mounting stacks now refuse to deploy without
  their drives.** On reboot, Docker restarts containers before macOS has
  mounted the external drives, so both deploy scripts now carry a mount 
  guard.
- Make 1Password service mode server-only.

## [0.12.0] - 2026-08-30

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

## [0.11.3] - 2026-08-29

### Changed

- **Backups moved to `/Volumes/Data2`.** All five backup scripts and their
  mount guards now target the second physical drive. Data1 held 1.5 TB of
  media *and* every backup, so a single drive failure took both; they are
  now decorrelated. Not a second copy — the drives share an enclosure — but
  a cheap improvement while restic/B2 is still on the roadmap.

## [0.11.2] - 2026-08-29

### Fixed

- Kestra upgrade script can now be run over ssh.

## [0.11.1] - 2026-08-29

### Fixed

- **Healthchecks for kestra and garage**, so every container in the lab now 
  has one.


## [0.11.0] - 2026-08-29

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

- **The Brewfile declares; it no longer installs.** It moves out of
  `.chezmoitemplates` and renders to `~/.Brewfile`.
- A new `lab.chezmoi/packages` flow is chained on the tick, and installs what's missing.
- A new `lab.chezmoi/packages-upgrade` flow runs nightly at 04:20, and moves versions forward.
- Chezmoi apply now installs the brewfile every time on non-server devices.

### Fixed

- **Caddy's healthcheck had never once passed** (`FailingStreak: 3933`).
  busybox `wget` resolves `localhost` to `::1` first, and caddy's admin API
  binds `127.0.0.1` only. Caddy was serving fine throughout — nothing in
  this lab reads Docker healthchecks, which is the gap `lab.beszel/health`
  closes.

## [0.10.2] - 2026-08-29

### Changed

- **The flow naming convention is enforced**, not just documented. A
  `lifecycle.precondition` on `kestra_flow` asserts that each flow's declared
  `namespace`/`id` match what its path requires (`<slice>/flows/<job>/` ->
  `lab.<slice>/<job>`).
- Every task now has a timeout.

## [0.10.1] - 2026-08-28

### Changed

- **The heartbeat is its own flow** (`lab.chezmoi/heartbeat`), chained on the
  tick's SUCCESS instead of being a task on the tick.

### Added

- **Task timeouts, where the bound is justifiable.** A hung task is worse
  than a failed one: no logs, no failure, no alert, and with
  `concurrency: QUEUE` another execution stacks up every interval behind it.

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

## [0.10.0] - 2026-08-28

### Added

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

### Changed

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

### Fixed

- The kestra secrets table still called the postgres item `Kestra Postgres`.
  0.9.2 fixed the templates to `kestra-postgres` but not the doc.

## [0.9.2] - 2026-08-28

### Changed

- **SSL maintenance moved out of caddy**: certificates now come from a nightly `renew-certs` Kestra job.
- The caddy container no longer carries any secret: `caddy.env` (NETLIFY_TOKEN) is consumed only by the renew-certs job.

### Removed

- `caddy/Dockerfile`, the xcaddy build, and the libdns patch apparatus — the stock `caddy:2.10` image is enough now that caddy does no ACME. The headless buildx/credential machinery stays (general-purpose; uncached pulls still need the null helper).

### Fixed

- The two kestra env templates disagreed on the postgres item after the kebab-case renames (`Kestra Postgres` vs `kestra-postgres`) — the next rotation or fresh bootstrap would have failed on whichever name no longer existed. Both now read `kestra-postgres`.
- `forgejo/tofu` and `kestra/tofu` state encryption is now `enforced = true` (migrations done): tofu refuses unencrypted state instead of silently accepting it, and the leftover migration-era `unencrypted` method declarations are gone.

## [0.9.1] - 2026-08-27

### Fixed

- Chezmoi should now use the 1Password service account for fetching credentials.
- `setup.sh` now converges caddy FIRST (with `--build`) — its compose owns the shared `lab` network, and every other stack's `external: true` reference fails until it exists.
- The wildcard certificate actually issues now: libdns/netlify v1.2.0 types a DNS zone's `domain` as string (via Netlify's stale open-api models), but for domains REGISTERED THROUGH Netlify the live API returns an object there.
- Headless builds work through the job bridge — the same macOS-headless trap as 0.5.x/0.6.0, on the build path instead of pull, needing TWO fixes. (1) `~/.docker-headless/cli-plugins/docker-buildx` is now a chezmoi-managed symlink to Docker Desktop's plugin: without buildx, compose falls back to the legacy builder outright. (2) The headless Docker config now declares `credsStore: "headless"`, a chezmoi-managed null helper in `~/.docker-headless/bin` (on lab-job's PATH, first) that answers every lookup with "not found" so registry access proceeds anonymously. WHY the bare `{}` config wasn't enough: with no credsStore, the Docker CLI's platform *default* on macOS is osxkeychain whenever that binary is on PATH — so any not-yet-cached image (like a build's base image) hit the locked login keychain (`error getting credentials … keychain cannot be accessed`). Existing stacks never noticed because their pinned images were already local.

## [0.9.0] - 2026-08-27

### Added

- **The front door**: new `caddy` slice — Caddy (custom build with the Netlify DNS-01 module) terminating TLS on `:443` with a single `*.lab.twolfe.dev` wildcard certificate (one cert on purpose: per-name certs would publish every internal hostname to Certificate Transparency logs). The wildcard DNS record lives in `caddy/tofu` (netlify provider — a provider, not a slice; public per-service records will live in the owning slice's tofu). Routes are slice-owned: each service contributes a `caddy.caddyfile` picked up by an import glob, same contract as `flows/` and the job bridge. Services now answer at `https://<name>.lab.twolfe.dev` (see ENDPOINTS.md); port publishes stay as the automation path and fallback.
- Shared `lab` Docker network, owned by the caddy slice; forgejo, kestra, jellyfin and garage join it (kestra keeps `default` too — postgres lives there).
- **State encryption**: OpenTofu client-side encryption (PBKDF2 + AES-GCM) on every Garage-backed tofu root — Garage has no SSE, and state holds secrets. New `caddy` root is born enforced; `forgejo` and `kestra` carry an unencrypted fallback until their first post-change state write, then the fallback comes out (see each `encryption.tf`). Passphrase: 1P `tofu-state-passphrase`. The garage root is exempt — its state is local and deliberately disposable.

### Changed

- Forgejo's identity is now `https://forgejo.lab.twolfe.dev/` (ROOT_URL, DOMAIN, SSH_DOMAIN) — links and clone URLs advertise the proxied name; `macmini.local:3000` still works via the published port.
- The secrets-bootstrap scripts (garage, kestra — and caddy's, which never shipped) collapsed into chezmoi `create_` templates under `chezmoi/home/Docker/`: same semantics (1Password is the origin, the env file is a cache, written only when missing), one declarative layer instead of a script writing a file. Verified: chezmoi never evaluates a `create_` template whose target exists, so `op`/internet stay bootstrap-only dependencies — and the update flow now exports the mini's 1P service account so a headless tick can re-materialize a deleted cache. The kestra script never actually generated anything (the vault-is-origin fix predates this); the stale compose comment claiming it did is gone too.

## [0.8.0]

### Added

- Added [BetterDisplay](https://formulae.brew.sh/cask/betterdisplay) to Chezmoi, installed on desktops only.

### Fixed

- Installed BetterDisplay to hopefully try and stop monitors moving around every time I plug them into the dock.

## [0.7.2] - 2026-08-27

### Fixed

- Pull mirrors no longer trigger Actions runs: every feature unit (actions, issues, PRs, wiki, packages, projects, releases) is now off on all 33 mirrors — mirrors are read-only copies, GitHub owns their features. Declared in `mirrors.tf`; Hamelin needed the change applied via a minimal API PATCH because the provider's full-object PATCH 500s on repos with wikis (empty `wiki_branch` = branch rename to `""` — now documented in the forgejo README with the workaround).
- Removed `permissions` from `ignore_changes` (computed-only in the current provider; OpenTofu flags it as redundant).

## [0.7.1] - 2026-08-26

### Fixed

- Chezmoi now `init`s before updating.
- Removed untracked `permissions` attributes causing warnings on the forgejo tofu stack.

## [0.7.0] - 2026-08-26

### Added

- Nightly scheduled backups, staggered clear of the tick's quarter-hour columns. All land on the external drive at `/Volumes/Data1/backups/<thing>`; each keeps the last 10, and kestra's `pre-<version>` upgrade dumps are exempt from pruning.
- `backup-garage` 02:50 (cold copy of `meta/` + `data/`), 
- `backup-forgejo` 03:05 (cold copy of `data/`, ~30s downtime, image tag recorded beside each archive), 
- `backup-kestra` 03:20 (live `pg_dump`, no downtime). 
- `backup-jellyfin` 03:35 (cold copy of config + database + library roots + plugins. 
- The cold backups guard against the deploy race: if the stack is restarted mid-tar, the archive is discarded and the run fails loudly rather than keeping a suspect copy. 
- Forgejo's and Jellyfin's backup scripts now live in-repo (`<svc>/flows/backup/script.sh`).

### Changed

- The tick now runs `chezmoi update --init`: the config file is derived state, so regenerate it when its template changes instead of warning on every apply forever. Headless-safe because the config template only uses `promptChoiceOnce`.
- Removed the one-time `moved` blocks from `kestra/tofu/flows.tf` now the first post-restructure apply has migrated the state keys.

## [0.6.0] - 2026-08-26

### Changed

- **Vertical slicing**: the repo is now organized by *thing* rather than by *tool*. `compose/<svc>`, `tofu/<svc>` and stray script directories merged into per-thing slices at the repo root.
- `tofu/kestra` + `jobs/` became one directory per job (`<slice>/flows/<job>/flow.yaml` + `script.sh` side by side.
- **Chezmoi declares, Kestra acts**: the `run_onchange` deploy hook is gone. Each service now has a `deploy-<svc>` Kestra flow chaining on `chezmoi-update` SUCCESS.
- The chezmoi tick runs every 15 minutes (was hourly). With no post-merge poke yet, the tick is the only delivery path.
- `lab-job` names are now two-segment `slice/job` paths (e.g. `forgejo/deploy`, `obsidian/main`) resolved to `<slice>/flows/<job>/script.sh` at the repo root. Job names no longer need to be globally unique.
- Garage cluster layout init moved out of chezmoi (`run_once` deleted) into `garage/scripts/init-layout.sh`, invoked by the new `setup.sh`.

### Added

- `setup.sh`: fresh-server bring-up. Imperative bootstrap, since Kestra can't deploy itself into existence.
- `kestra/scripts/upgrade.sh`: manual, guarded kestra upgrade (kestra deliberately has **no** deploy flow, because it can't safely replace its own executor). Takes a Postgres backup first.
- Janitor flow (`kestra/flows/purge/flow.yaml`): nightly purge of >30-day execution history — the 15-minute tick fan-out would otherwise grow postgres forever.
- `kestra/flows/backup/script.sh`: dated `pg_dump` of the kestra DB (husk-proof: dumps to `.partial`, renames on success). Callable by hand or as `kestra/backup` through the bridge — flow-ready for a future scheduled backup; `upgrade.sh` delegates to it with a `pre-<version>` label.

### Removed

- `DOCKER_HOST` hack that never fixed anything.

### Fixed

- Compose stacks actually converge through the Kestra bridge now. `docker compose` is a CLI plugin resolved through `$DOCKER_CONFIG/cli-plugins` (never PATH), and Docker Desktop on macOS ships its plugins only in `~/.docker/cli-plugins`.

## [0.5.1] - 2026-08-25

### Fixed

- `lab-job` now pins `DOCKER_HOST` to Docker Desktop's user-level socket. The headless `DOCKER_CONFIG` has no contexts store, so bridge jobs fell back to the privileged `/var/run/docker.sock` symlink, which isn't guaranteed to exist.

## [0.5.0] - 2026-08-25

### Added

- Added new `git aliases` alias for outputting existing aliases.
- There is a new `forgejo.scripts/import.sh` script used to import some old repositories. Kept for posterity.

### Changed

- Forgejo PRs should now default to Squash.
- Forgejo instance is now named `WolfeForge`.

## [0.4.1] - 2026-08-25

### Fixed

- The Foregejo mirrors had incorrectly configured PATs, so I've recreated them.
- The OpenTofu stack now correctly ignores driftable config like `internal_tracker` and `permissions` because the provider doesn't keep them stable. 

## [0.4.0] - 2026-08-25

### Added

- Lots of git aliases: `git undo` unwinds the last commit, `git main` puts you back on latest main, `git sweep` removes merged branches, and `git catchup` applies the latest changes from `main`.

### Changes

- Git fetch now automatically prunes.

## [0.3.2] - 2026-08-25

### Removed

- Removed old launchd `ob sync` scripts now that sync is done through Kestra instead.

## [0.3.1] - 2026-08-25

### Fixed

- Node LTS is now installed and aliased as nvm's default by chezmoi. The server script also reinstalls global npm tools into the new version.
- `nvm-run` now warns loudly when it falls back to the default alias instead of hiding it.


## [0.3.0] - 2026-08-24

### Added

- 1Password Service Account support. 1Password now authenticates using a service account, if the token for one is saved at `~/Docker/1password/service-account-token` (chmod 600). (Service account has also been configured on the server)

### Changed

- The Postgres health check now does `pg_isready` then continues with a `SELECT 1` to test the database is actually reachable. 

### Fixed

- The timezone setting to match to the host didn't work, so just set the timezone literally.
- Run Postgres as the host user so it works properly.

## [0.2.4] - 2026-08-24

### Fixed

- The timezone setting to match to the host didn't work, so just set the timezone literally.

## [0.2.3] - 2026-08-24

### Fixed

- The bootstrap kestra secrets script had a logic error in it that caused the env file to get written before the SSH key existed, so it was left blank. 

## [0.2.2] - 2026-08-24

### Fixed

- The 1Password secrets should now use the correct field names: `/password` for passwords, `/credential` for keys.

## [0.2.1] - 2026-08-24

### Fixed

- The 1Password secrets now reference the correct vault name of `Wolfe.Lab` instead of `Personal`.

## [0.2.0] - 2026-08-24

### Added

- `compose/kestra`: Kestra job scheduler (+ Postgres) on the mini, replacing launchd's scheduling
- `tofu/kestra`: every flow as YAML in the repo, applied declaratively
- SSH job bridge: Kestra reaches the host only through a forced-command key (`restrict,command=lab-job`) that resolves job names to `jobs/*.sh` in the repo checkout
- Flows: `chezmoi-update` (hourly + CI-pokeable webhook), `obsidian-sync-main` and `obsidian-sync-dnd` (one-shot passes every 10 minutes, replacing the launch agents after cutover)
- Headless `DOCKER_CONFIG` (`~/.docker-headless`, no osxkeychain credsStore) so registry pulls work in SSH sessions without the login keychain

### Changed

- Runtime secrets are now materialized from 1Password, never generated on-machine. (Forgejo and Jellyfin's admin passwords are DB state, not env secrets.)

## [0.1.1] - 2026-08-24

### Fixed

- The `TheBoredTeam/boring-notch/boring-notch` is now trusted correctly.

## [0.1.0] - 2026-08-23

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

## [0.0.1] - 2026-08-22

### Added

- Initial repository: Brewfiles and terminal profile
