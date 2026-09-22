# Wolfe.Lab runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## New machine bootstrap

1. Install 1Password and sign in (its SSH agent provides git auth).
2. Run:
   ```sh
   sh -c "$(curl -fsLS get.chezmoi.io)" -- init --ssh --apply tom-wolfe/Wolfe.Lab
   ```
3. You'll be asked which machine this is — `macbook`, `macstudio`,
   `work-macbook`, `macmini-node` or `pi-node` — and every file in the source says what
   that profile gets (`chezmoi/README.md` "Profiles").
4. Servers additionally: `./setup.sh` from the checkout to bring the stacks up and hand convergence over to Forgejo Actions — the script header documents the details.

If the machine has (or later gets) a working copy at `~/Development/Wolfe/Wolfe.Lab`, chezmoi uses it as the source automatically after `chezmoi init` — otherwise it manages its own clone in `~/.local/share/chezmoi`.

## Secondary node bootstrap

The Macs above; a Linux node (the Pi) has no 1Password app, no Homebrew
and no GUI, so it is the same idea with different mechanics. In order:

1. **Image and boot.** Debian (arm64) on the NVMe; the EEPROM boot order
   prefers NVMe with the SD card as fallback (`sudo rpi-eeprom-config
   --edit`, `BOOT_ORDER=0xf416`). Keep the SD card, labelled — it is the
   console of last resort for a machine with no display.
2. **Docker Engine** from Docker's apt repository; `sudo usermod -aG
   docker $USER` and log in again. `loginctl enable-linger $USER`, so user
   services (the runners, the Beszel agent) start at boot with nobody
   logged in.
3. **Tailscale**, the native Linux client: `tailscale up`, then add the
   node to `servers` in `tailscale/tofu/variables.tf` (a normal PR) — that
   tags it and disables its key expiry.
4. **The chezmoi source's deploy key**: `ssh-keygen -t ed25519 -f
   ~/.ssh/forgejo_deploy`, and the public half as a read-only deploy key
   on the Wolfe.Lab repo in Forgejo (Settings → Deploy keys). This is the
   one key made on the machine: chezmoi needs it before chezmoi runs.
5. **The bootstrap secret**: `export OP_SERVICE_ACCOUNT_TOKEN=…` (the
   vault's service account) — the `create_` templates read the vault on
   this first apply. It persists in the runner's env file (next step).
6. `sh -c "$(curl -fsLS get.chezmoi.io)" -- init --apply
   git@git.twolfe.dev:tom-wolfe/Wolfe.Lab.git` — answer `pi-node`. That
   profile gets no Homebrew and no macOS scripts; binaries arrive as
   pinned externals under `~/.local/bin`.
7. Then, per slice: the runners (`forgejo/RUNBOOK.md` "Bringing up the
   Pi"), the Beszel agent's hub-side setup (`beszel/RUNBOOK.md`), the
   backup path (`restic/RUNBOOK.md` "A Linux node").

## Manual sign-ins (not automatable)

Auth state is device-bound by design; these are the once-per-machine rituals:

- [ ] **1Password** — first, always: unlocks SSH/git, and everything below
- [ ] **Full Disk Access** (server) — grant to **Terminal** (the op CLI discovers
      the desktop app by reading its TCC-protected group container; without this
      it silently falls back to manual sign-ins)
- [ ] **1Password service account** (server) — create at 1password.com
      (Developer → Service Accounts), read-only grant on the **Wolfe.Lab vault
      only**, and place the token at
      `~/Docker/1password/service-account-token` (chmod 600). The bootstrap
      scripts prefer it over the desktop-app session — prompt-free and works
      over SSH; revoke/rotate from 1password.com any time
- [ ] **App Store** — required before `mas` apps in the Brewfile will install
- [ ] **`gh auth login`** — per-machine token, stays out of the repo
- [ ] **Obsidian Sync** — per vault; check the "Vault configuration" sync toggles.
      The server's vaults are re-pointed by `obsidian/RUNBOOK.md`, not signed in here
- [ ] Browser profiles, Slack (work), App Store SSO authorization for org repos as needed

## After setup.sh

The bring-up script converges the stacks; these cannot be scripted and
are done once per fresh server, in order.

  1. Register this machine's Actions runner (step 3 below) and run the
     heartbeat workflow once from the Actions tab — from then on
     healthchecks.io knows the lab's scheduler is alive.

  2. Enrol the monitoring agent — the one bootstrap that can't be ordered
     ahead of time, because the hub mints the token the agent needs:
       http://macmini.local:8090 -> create the superuser
       Settings -> Tokens -> copy the universal token and public key into
         a 1Password item `beszel-agent` (credential / username)
       chezmoi apply && brew services list
     Then set thresholds and the Pushover URL in the hub — it ships none,
     so nothing alerts until you do. Full runbook: beszel/README.md.

  3. Register every node's Actions runner, this machine's included — registrations live in
     Forgejo's database, so a fresh Forgejo knows none of them, while each
     node's runner.json still holds its vault secret and will poll with it
     until the server knows it again (forgejo/README.md "Runners"):
       cd forgejo
       dotnet run --project ../build/src/Wolfe.Lab.Build -- register-runner --node MacMini
       dotnet run --project ../build/src/Wolfe.Lab.Build -- register-runner --node wolfe-pi5
       dotnet run --project ../build/src/Wolfe.Lab.Build -- register-runner --node wolfe-pi5 --kind docker
     Same secret, same UUID: the node side needs no change.

  4. Repoint the chezmoi checkout at the primary: the bootstrap cloned the
     GitHub mirror, and CD should not depend on it.
       git -C ~/.local/share/chezmoi remote set-url origin http://localhost:3000/tom-wolfe/Wolfe.Lab.git
