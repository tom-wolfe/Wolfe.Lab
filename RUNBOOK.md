# Wolfe.Lab runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## New machine bootstrap

1. Install 1Password and sign in (its SSH agent provides git auth).
2. Run:
   ```sh
   sh -c "$(curl -fsLS get.chezmoi.io)" -- init --ssh --apply tom-wolfe/Wolfe.Lab
   ```
3. You'll be asked what kind of machine it is (`personal` / `work` / `server`), which controls the apps that get installed.
4. Servers additionally: `./setup.sh` from the checkout to bring the stacks up and hand convergence over to Forgejo Actions — the script header documents the details.

If the machine has (or later gets) a working copy at `~/Development/Wolfe/Wolfe.Lab`, chezmoi uses it as the source automatically after `chezmoi init` — otherwise it manages its own clone in `~/.local/share/chezmoi`.

## Manual sign-ins (not automatable)

Auth state is device-bound by design; these are the once-per-machine rituals:

- [ ] **1Password** — first, always: unlocks SSH/git, and everything below
- [ ] **Full Disk Access** (server) — grant to **Terminal** (the op CLI discovers
      the desktop app by reading its TCC-protected group container; without this
      it silently falls back to manual sign-ins) and to the **Actions runner
      binary** (`/opt/homebrew/opt/forgejo-runner/bin/forgejo-runner`): the
      obsidian syncs it runs touch `~/Library/CloudStorage`, and a
      launchd-started process cannot be prompted
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

## After setup.sh

The bring-up script converges the stacks; these cannot be scripted and
are done once per fresh server, in order.

  1. Register this machine's Actions runner (step 3 below) and run the
     heartbeat workflow once from the Actions tab — from then on
     healthchecks.io knows the lab's scheduler is alive.

  2. Grant Full Disk Access to the runner binary
     (/opt/homebrew/opt/forgejo-runner/bin/forgejo-runner) in System
     Settings → Privacy: the obsidian syncs read ~/Library/CloudStorage,
     which TCC guards, and a launchd-started process cannot be prompted.

  3. Enrol the monitoring agent — the one bootstrap that can't be ordered
     ahead of time, because the hub mints the token the agent needs:
       http://macmini.local:8090 -> create the superuser
       Settings -> Tokens -> copy the universal token and public key into
         a 1Password item `beszel-agent` (credential / username)
       chezmoi apply && brew services list
     Then set thresholds and the Pushover URL in the hub — it ships none,
     so nothing alerts until you do. Full runbook: beszel/README.md.

  4. Register every node's Actions runner, this machine's included — registrations live in
     Forgejo's database, so a fresh Forgejo knows none of them, while each
     node's runner.json still holds its vault secret and will poll with it
     until the server knows it again (forgejo/README.md "Runners"):
       forgejo/scripts/register-runner.sh MacMini
       forgejo/scripts/register-runner.sh wolfe-pi5
     Same secret, same UUID: the node side needs no change.
