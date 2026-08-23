# Wolfe.Config

Monorepo for my machine and homelab configuration.

## Layout

| Path | Purpose |
| --- | --- |
| `home/` | chezmoi source: dotfiles, Brewfile, and setup scripts |
| `compose/` | *(planned)* docker compose stacks for the Mac mini |
| `k8s/` | *(planned)* Argo CD applications and manifests |

## New machine bootstrap

1. Install 1Password and sign in (its SSH agent provides git auth).
2. Run:
   ```sh
   sh -c "$(curl -fsLS get.chezmoi.io)" -- init --ssh --apply trwolfe13/Wolfe.Config
   ```
3. You'll be asked what kind of machine it is (`personal` / `work` / `server`), which controls the apps that get installed.

If the machine has (or later gets) a working copy at `~/Development/Wolfe/Wolfe.Config`, chezmoi uses it as the source automatically after `chezmoi init` — otherwise it manages its own clone in `~/.local/share/chezmoi`.

## Day-to-day

```sh
chezmoi diff       # preview what apply would change
chezmoi apply      # apply dotfiles + run scripts (brew bundle runs when the Brewfile changed)
chezmoi update     # pull the repo and apply
chezmoi add ~/.zshrc   # start managing a new dotfile
```

Edit the Brewfile at `home/.chezmoitemplates/Brewfile`, then `chezmoi apply` — the install script re-runs whenever its rendered content changes.
