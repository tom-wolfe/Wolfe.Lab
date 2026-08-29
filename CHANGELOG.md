# Changelog

All notable changes to the lab are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); entries are dated
rather than versioned — the lab is continuous, not released.

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
