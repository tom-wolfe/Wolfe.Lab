# Changelog

All notable changes to the lab are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); entries are dated
rather than versioned — the lab is continuous, not released.

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
- `lab-job` names are now two-segment `slice/job` paths (e.g. `forgejo/deploy`, `obsidian-sync/main`) resolved to `<slice>/flows/<job>/script.sh` at the repo root. Job names no longer need to be globally unique.
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
