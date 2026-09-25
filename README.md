# Wolfe.Lab

Monorepo for my machine and homelab configuration!

## Layout

Vertically sliced: everything the lab runs is one directory at the repo
root, whatever mix of compose, tofu and jobs it needs — even
chezmoi is a slice, holding the declarative machine plane (`home/`) beside
its tick job.

| Path                               | Purpose                                                                                                                                                                                                                                                               |
|------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `<name>/`                          | one slice per thing the lab runs: a folder of *components*, each a directory with a `ritten.json` naming its shape — `compose/` (the stack, its secrets references and its route snippet), `backup/` (what restic keeps of it), `tofu/` (its API resources), and one of its own for anything only that slice does — plus one README and its runbook. The schedule is the workflow in `.forgejo/workflows/<slice>-<component>.yaml` |
| `chezmoi/home/`                    | the chezmoi source — dotfiles, the Brewfile, the runner and agent files a node's own services read at start: everything *declarative* about a machine (`.chezmoiroot` points here)                                                                                    |
| `build/`                           | the lab's jobs as a CLI, `lab`, built on Ritten: one workflow per component shape, run from the component's directory                                                                                                    |
| `setup.sh`                         | fresh-server bring-up — the one imperative bootstrap (Forgejo can't deploy itself into existence)                                                                                                                                                                     |
| `k8s/`                             | *(planned)* Argo CD applications and manifests                                                                                                                                                                                                                        |
| `RUNBOOK.md`, `<slice>/RUNBOOK.md` | procedures — bootstrap, upgrade, backup, restore — kept apart from the design prose so they can be followed step by step                                                                                                                                              |
| `ENDPOINTS.md`                     | every service address in the lab                                                                                                                                                                                                                                      |
| `CHANGELOG.md`                     | what changed, when — Keep a Changelog format                                                                                                                                                                                                                          |
| `ROADMAP.md`                       | what's next and why — including what's deliberately deferred                                                                                                                                                                                                          |

## Nodes

- **Servers** — the mini (the primary: the drives, and every scheduled
  job) and the Pi. `tag:server`; their failure is an outage, and they
  are watched as such.
- **The hybrid** — the Studio: the primary workstation, on when it is
  being used and off otherwise, serving while it is on. One rule keeps
  that safe: **the Studio may serve, but nothing may depend on it.**
  Anything it provides degrades rather than breaks when it sleeps —
  today the upper tier of models behind `ai.twolfe.dev`
  (`ollama/README.md`, "The Studio"). Its own tag, `tag:hybrid`, so the
  policy lets in the ports it serves and nothing else
  (`tailscale/README.md`); a host runner holding no privacy grants, whose
  jobs queue while it sleeps (`forgejo/README.md`, "The Studio's
  runner"); a Beszel agent and a Gatus check that never alert. Nothing in
  the platform layer, nothing on the drives, and no job whose failure is
  an outage runs there — which is why Immich's machine learning stays on
  the mini (`immich/README.md`).

## How deployment works

Chezmoi declares; Forgejo Actions acts. Every component has a workflow in
`.forgejo/workflows/<slice>-<component>.yaml` that fires on the push
touching it, runs on the node the slice lives on (`runs-on` is placement
— the mini's host runner or the Pi's), checks the repo out into a
disposable workspace, and runs `lab deploy` from the component's
directory. For a compose component that means: install it under its
release name, `~/.local/share/Wolfe.Lab/<release>` (containers
bind-mount files from it after the job is gone), and `docker compose up
-d` there. `docker compose up -d` is convergent, so a manual run is
always safe. The same workflow checks the compose file on every pull
request, and a tofu root's does the same with a plan. Nothing ticks.

Secrets never sit in a file. A component that needs one commits a
`secrets.env` of vault *references* beside its compose file, and the
deploy resolves them into the environment of the one `compose up`
that creates the container — through the CLI's secrets provider, the
only thing in the repo that speaks to the vault, so changing vaults
changes one registration. A container keeps the environment it was created with, so a
stack runs, restarts and reboots with no vault in the loop; a rotated
value reaches it on the next deploy (re-run the workflow). Jobs —
backups, certificate renewal, the alerts, every tofu root — resolve
their own env files the same way at run time. The vault is reachable or
a deploy fails; a running stack never notices.

Machine config is chezmoi's and separate: `.forgejo/workflows/chezmoi.yaml`
runs `chezmoi update` on a node when `chezmoi/` changes. Everything
scheduled — the nightly backups, restic's offsite copy and weekly verify,
the obsidian syncs, the Immich deploy and import, certificate renewal, the heartbeat and the Gatus probe
— is a cron-triggered workflow on the mini's host runner, over the same
scripts. Nothing ticks, nothing chains: one workflow per job, each with
its own schedule and its own failure alert.

## How monitoring works

Five layers, deliberately, because they fail in different ways. The rule
that orders them: **a watcher must not share the fate of the thing it
watches.**

| Layer                         | Watches                                                                                                                            | Dies when                         |
|-------------------------------|------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------|
| every job                     | its own failure → Pushover, from the CLI's runtime                                                                                                  | Forgejo or the node's runner does |
| Beszel agent                  | each node's CPU, memory, disks (incl. `/Volumes/Data1`), containers                                                                | that node does                    |
| Gatus (`gatus/`, on the Pi)   | every service by REQUEST, once each through the front door, plus the third parties the lab stands on; the Beszel hub among them | the Pi does                       |
| `gatus-health.yaml`           | Gatus itself, from the mini — a dead status page looks like one you haven't opened                                                 | the mini does                     |
| healthchecks.io               | the heartbeat still pings → **the only observer outside the building**                                                             | never (it's SaaS)                 |
| `heartbeat/` (`heartbeat.yaml`) | sends that ping every 15 minutes from the mini's runner — proof Forgejo, the runner and its schedules are alive                    | the mini does                     |

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
on the mini that's the chezmoi workflow (`00-install-packages.sh` on apply). Nothing upgrades
automatically on the mini: versions move when you run `brew bundle install
--file ~/.Brewfile --upgrade` there. See `chezmoi/README.md`.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
