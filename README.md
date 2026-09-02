# Wolfe.Lab

Monorepo for my machine and homelab configuration!

## Layout

Vertically sliced: everything the lab runs is one directory at the repo
root, whatever mix of compose, tofu, flows and scripts it needs — even
chezmoi is a slice, holding the declarative machine plane (`home/`) beside
its tick job.

| Path | Purpose |
| --- | --- |
| `<name>/` | one slice per thing the lab runs: a compose stack + configs and/or Kestra jobs (`flows/<job>/` directories, `flow.yaml` + `script.sh` paired), a `tofu/` root where the service has API resources, one README |
| `chezmoi/home/` | the chezmoi source — dotfiles, the Brewfile, secrets-bootstrap templates: everything *declarative* about a machine (`.chezmoiroot` points here) |
| `setup.sh` | fresh-server bring-up — the one imperative bootstrap (Kestra can't deploy itself into existence) |
| `k8s/` | *(planned)* Argo CD applications and manifests |
| `ENDPOINTS.md` | every service address in the lab |
| `CHANGELOG.md` | what changed, when — Keep a Changelog format |
| `ROADMAP.md` | what's next and why — including what's deliberately deferred |

## How deployment works

Chezmoi declares; Kestra acts. The `lab.chezmoi/update` flow is the CD tick:
every 15 minutes it pulls the repo and converges machine config, and each
service's `lab.<slice>/deploy` flow chains on its SUCCESS — pull → config →
deploys, always in that order. `docker compose up -d` is convergent, so
between changes the deploys are no-ops; a merged compose change lands
within one tick. Kestra itself has no deploy flow (it can't safely replace
its own executor) — see `kestra/README.md` for the manual upgrade path.
All flows are applied from `kestra/tofu/` — kestra's tofu root manages
flows the way forgejo's manages repositories and garage's manages buckets.

## How monitoring works

Five layers, deliberately, because they fail in different ways. The rule
that orders them: **a watcher must not share the fate of the thing it
watches.**

| Layer | Watches | Dies when |
| --- | --- | --- |
| `system/alert-failed` | every `lab.*` flow failure → Pushover | Kestra does |
| Beszel agent | the mini's CPU, memory, disks (incl. `/Volumes/Data1`), containers | the mini does |
| `lab.beszel/health` | the Beszel hub itself — a dead monitor looks like a healthy lab | Kestra does |
| Gatus (`gatus/`) | every service by REQUEST — direct and through the front door — plus the third parties the lab stands on | the mini does |
| `lab.gatus/health` | Gatus itself — a dead status page looks like one you haven't opened | Kestra does |
| healthchecks.io | the tick still pings → **the only observer outside the building** | never (it's SaaS) |
| `lab.chezmoi/heartbeat` | sends that ping, chained on the tick so it can't break it | Kestra does |

Everything except healthchecks.io runs inside the lab, so a dead mini is
silence from all of them — and silence is indistinguishable from health.
That is the entire reason the dead man's switch is off-site, and the reason
Gatus *adds* to this list rather than replacing anything in it.

Gatus is what catches a container that is up but wedged — only a request
finds that, and it is how caddy's healthcheck sat red for 33 hours while
caddy served fine. The one thing still nothing catches is a flow that
hangs rather than fails, which is why every flow carries a `timeout`.

## New machine bootstrap

1. Install 1Password and sign in (its SSH agent provides git auth).
2. Run:
   ```sh
   sh -c "$(curl -fsLS get.chezmoi.io)" -- init --ssh --apply tom-wolfe/Wolfe.Lab
   ```
3. You'll be asked what kind of machine it is (`personal` / `work` / `server`), which controls the apps that get installed.
4. Servers additionally: run `chezmoi apply` a second time (authorized_keys can only template the job-bridge key after the first apply materializes it), then `./setup.sh` from the checkout to bring the stacks up and hand convergence over to Kestra — the script header documents the details.

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
chezmoi apply      # apply dotfiles + run scripts (laptops: installs missing brew packages)
chezmoi update --init   # pull the repo and apply (--init: regenerate config if its template changed)
chezmoi add ~/.zshrc   # start managing a new dotfile
```

Edit the Brewfile at `chezmoi/home/dot_Brewfile.tmpl`. chezmoi renders it to
`~/.Brewfile` and stops there — it *declares* the package set, it does not
install it. On a laptop the apply-time script installs whatever is missing;
on the mini that's the `lab.chezmoi/packages` flow, with
`lab.chezmoi/packages-upgrade` moving versions nightly. See
`chezmoi/README.md`.
