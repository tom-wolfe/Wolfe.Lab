# Wolfe.Lab

Monorepo for my machine and homelab configuration!

## Layout

Vertically sliced: everything the lab runs is one directory at the repo
root, whatever mix of compose, tofu, flows and scripts it needs — even
chezmoi is a slice, holding the declarative machine plane (`home/`) beside
its tick job.

| Path | Purpose |
| --- | --- |
| `<name>/` | one slice per thing the lab runs: a compose stack + configs and/or jobs (`flows/<job>/` directories holding the job's script or `backup.conf`; the schedule is the workflow in `.forgejo/workflows/`), a `tofu/` root where the service has API resources, one README |
| `chezmoi/home/` | the chezmoi source — dotfiles, the Brewfile, secrets-bootstrap templates: everything *declarative* about a machine (`.chezmoiroot` points here) |
| `setup.sh` | fresh-server bring-up — the one imperative bootstrap (Forgejo can't deploy itself into existence) |
| `k8s/` | *(planned)* Argo CD applications and manifests |
| `RUNBOOK.md`, `<slice>/RUNBOOK.md` | procedures — bootstrap, upgrade, backup, restore — kept apart from the design prose so they can be followed step by step |
| `ENDPOINTS.md` | every service address in the lab |
| `CHANGELOG.md` | what changed, when — Keep a Changelog format |
| `ROADMAP.md` | what's next and why — including what's deliberately deferred |

## How deployment works

Chezmoi declares; Forgejo Actions acts. Every slice has a workflow in
`.forgejo/workflows/<slice>.yaml` that fires on the push touching it, runs
on the node the slice lives on (`runs-on` is placement — the mini's host
runner or the Pi's), checks the repo out into a disposable workspace, and
runs `scripts/deploy.sh <slice>`: install the slice into
`~/.local/share/Wolfe.Lab/<slice>` (containers bind-mount files from it
after the job is gone) and `docker compose up -d` there. `docker compose up -d` is convergent, so
a manual run is always safe. OpenTofu roots have `tofu-<root>.yaml`: apply
on the push that changes them, a daily plan for drift. Nothing ticks.

Machine config is chezmoi's and separate: `.forgejo/workflows/chezmoi.yaml`
runs `chezmoi update` on a node when `chezmoi/` changes. Everything
scheduled — the nightly backups, restic's offsite copy and weekly verify,
the obsidian syncs, certificate renewal, the heartbeat and the Gatus probe
— is a cron-triggered workflow on the mini's host runner, over the same
scripts. Nothing ticks, nothing chains: one workflow per job, each with
its own schedule and its own failure alert.

## How monitoring works

Five layers, deliberately, because they fail in different ways. The rule
that orders them: **a watcher must not share the fate of the thing it
watches.**

| Layer | Watches | Dies when |
| --- | --- | --- |
| every workflow's failure step | its own job failing → Pushover (`scripts/alert.sh`) | Forgejo or the node's runner does |
| Beszel agent | each node's CPU, memory, disks (incl. `/Volumes/Data1`), containers | that node does |
| Gatus (`gatus/`, on the Pi) | every service by REQUEST — direct and through the front door — plus the third parties the lab stands on; the Beszel hub among them | the Pi does |
| `gatus-health.yaml` | Gatus itself, from the mini — a dead status page looks like one you haven't opened | the mini does |
| healthchecks.io | the heartbeat still pings → **the only observer outside the building** | never (it's SaaS) |
| `heartbeat.yaml` | sends that ping every 15 minutes from the mini's runner — proof Forgejo, the runner and its schedules are alive | the mini does |

Everything except healthchecks.io runs inside the lab, so a dead mini is
silence from all of them — and silence is indistinguishable from health.
That is the entire reason the dead man's switch is off-site, and the reason
Gatus *adds* to this list rather than replacing anything in it.

Gatus is what catches a container that is up but wedged — only a request
finds that, and it is how caddy's healthcheck sat red for 33 hours while
caddy served fine. The one thing still nothing catches is a flow that
hangs rather than fails, which is why every flow carries a `timeout`.

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
on the mini that's the chezmoi workflow (`install-packages.sh` on apply). Nothing upgrades
automatically on the mini: versions move when you run `brew bundle install
--file ~/.Brewfile --upgrade` there. See `chezmoi/README.md`.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
