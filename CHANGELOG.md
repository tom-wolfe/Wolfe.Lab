# Changelog

All notable changes to the lab are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); entries are dated
rather than versioned — the lab is continuous, not released.

## [Unreleased]

## [2026-08-24]

### Added

- chezmoi-managed system configuration for all three machines: machine-aware
  Brewfile (packages, casks, mas apps, VS Code extensions, npm, dotnet
  tools), dotfiles (`.zshrc`, `.zprofile`, `.gitconfig`, `.ssh/config`),
  macOS defaults, Vorssaint preferences, and the Obsidian sync launch agents
  (server-scoped via `.chezmoiignore`)
- SSH access to the Mac mini: 1Password SSH agent everywhere, managed
  `authorized_keys`, commit signing, HTTPS→SSH rewrite for GitHub
- `compose/`: forgejo, jellyfin, and garage stacks deployed by
  `chezmoi update` on the server (`run_onchange` deploy hook)
- Garage S3-compatible object store (`compose/garage`) with chezmoi-driven
  bootstrap: secrets generation, cluster layout
- `tofu/bootstrap`: disposable-state project seeding the OpenTofu state
  store (bucket `tofu-state` + key), credentials vaulted in 1Password
- `tofu/forgejo`: all 34 GitHub repos managed declaratively — pull mirrors
  across the Forgejo orgs (Hamelin, NSchema, Ritten, DisasterCare) and
  personal account, with per-repo `mode` switch (mirror/active)
- Wolfe.Lab promoted to Forgejo-primary, push-mirrored to GitHub on every
  commit
- House secrets pattern: committed `secrets.env` files of `op://` references
  resolved at spawn by `op run`
- `ENDPOINTS.md` service address table; this changelog

### Changed

- Mac mini git auth moved from the 1Password agent to a read-only account
  deploy key (headless-safe); commit signing disabled on the server
- dotnet tooling switched from brew formula to the `dotnet-sdk` cask
  (self-registering runtime)

### Fixed

- zsh completion never initialised (`compinit`) — tab completion now works
  fleet-wide; nschema completion served statically from `site-functions`
- nvm double-initialisation; `.zprofile`/`.zshrc` responsibilities untangled
- Launch agent reload: bootout/bootstrap race, third-party agent scoping,
  wrong chezmoi script phase
- Non-interactive SSH PATH gaps (`/usr/local/bin`) in server scripts

## [2026-08-23]

### Added

- Initial repository: Brewfiles and terminal profile
