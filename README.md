# Wolfe.Lab

Monorepo for my machine and homelab configuration.

## Layout

| Path | Purpose |
| --- | --- |
| `home/` | chezmoi source: dotfiles, Brewfile, and setup scripts |
| `compose/` | docker compose stacks for the Mac mini — applied by `chezmoi update` on the server |
| `jobs/` | host-side job scripts, run on the mini by Kestra through the lab-job SSH bridge |
| `tofu/` | OpenTofu projects — state store bootstrap, Forgejo repositories |
| `k8s/` | *(planned)* Argo CD applications and manifests |
| `ENDPOINTS.md` | every service address in the lab |
| `CHANGELOG.md` | what changed, when — Keep a Changelog format |

## New machine bootstrap

1. Install 1Password and sign in (its SSH agent provides git auth).
2. Run:
   ```sh
   sh -c "$(curl -fsLS get.chezmoi.io)" -- init --ssh --apply tom-wolfe/Wolfe.Lab
   ```
3. You'll be asked what kind of machine it is (`personal` / `work` / `server`), which controls the apps that get installed.

If the machine has (or later gets) a working copy at `~/Development/Wolfe/Wolfe.Lab`, chezmoi uses it as the source automatically after `chezmoi init` — otherwise it manages its own clone in `~/.local/share/chezmoi`.

### Manual sign-ins (not automatable)

Auth state is device-bound by design; these are the once-per-machine rituals:

- [ ] **1Password** — first, always: unlocks SSH/git, and everything below
- [ ] **Full Disk Access** (server) — grant to **Terminal** (the op CLI discovers
      the desktop app by reading its TCC-protected group container; without this
      it silently falls back to manual sign-ins) and to **remote users** via
      Sharing → Remote Login (the Kestra job bridge runs over SSH and touches
      protected paths like `~/Library/CloudStorage`)
- [ ] **1Password service account** (server) — create at 1password.com
      (Developer → Service Accounts), read-only grant on the **Wolfe.Lab vault
      only**, and place the token at
      `~/Docker/1password/service-account-token` (chmod 600). The bootstrap
      scripts prefer it over the desktop-app session — prompt-free and works
      over SSH; revoke/rotate from 1password.com any time
- [ ] **App Store** — required before `mas` apps in the Brewfile will install
- [ ] **Google Drive** — personal + server (vault backups depend on it)
- [ ] **`gh auth login`** — per-machine token, stays out of the repo
- [ ] **Obsidian Sync** — per vault; check the "Vault configuration" sync toggles
- [ ] Browser profiles, Slack (work), App Store SSO authorization for org repos as needed

## Day-to-day

```sh
chezmoi diff       # preview what apply would change
chezmoi apply      # apply dotfiles + run scripts (brew bundle runs when the Brewfile changed)
chezmoi update     # pull the repo and apply
chezmoi add ~/.zshrc   # start managing a new dotfile
```

Edit the Brewfile at `home/.chezmoitemplates/Brewfile`, then `chezmoi apply` — the install script re-runs whenever its rendered content changes.
