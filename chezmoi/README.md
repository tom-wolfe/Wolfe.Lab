# chezmoi — the CD tick

The lab's game tick: `flows/update/flow.yaml` runs `chezmoi update` on the mini
every 15 minutes — pull the repo, converge machine config. It is the single
clock and the only place the repo gets pulled; every service's
`lab.<slice>/deploy` flow chains on this flow reaching SUCCESS, so the order is
always pull → config → deploys, and a broken config converge *pauses*
deployment (the next green tick converges everything — the deploys are
idempotent no-ops when nothing changed).

The whole chezmoi story lives in this slice: `home/` is the *source* — the
declarative machine plane itself (dotfiles, the Brewfile, the `create_`
secret-cache templates under `home/Docker/`; `.chezmoiroot` points chezmoi
at it) — and `flows/update/` is the job that runs it on the server.

## Packages

`home/dot_Brewfile.tmpl` renders to `~/.Brewfile` and stops there. It
*declares* the machine's package set. Acting on it:

| Job | When | Does |
| --- | --- | --- |
| `.chezmoiscripts/install-packages.sh` | every `chezmoi apply`, every machine | `brew bundle install --no-upgrade` — installs what's missing, nothing more |
| `.forgejo/workflows/chezmoi.yaml` | the push that touches `chezmoi/` | runs `chezmoi update` on each server, which is the apply above |
| you, at the desk | when you choose | `brew bundle install --file ~/.Brewfile --upgrade` on the mini — the only thing that moves versions there |

### Why installs and upgrades are separate

The Brewfile used to live *inside* a `run_onchange_` script, inlined by
`{{ template "Brewfile" . }}`. chezmoi keys those scripts on a hash of the
rendered script, so `brew bundle` ran when the Brewfile changed — and since
**`brew bundle` upgrades outdated formulae by default**, every version bump
on the mini was triggered by whatever unrelated edit happened to touch that
file. Adding one VS Code extension swept the entire toolchain along with it;
two quiet months would have frozen it silently. The mini was current only by
accident.

`--no-upgrade` is the whole fix: installing what is declared and moving
versions forward are different jobs with different reasons to happen.

### Why upgrades are manual

There is nothing to pin to. `brew "foo"` *means* "the current formula";
versioned formulae like `node@22` exist only where upstream publishes them
and are separate packages, not pins; casks and `mas` have no version
selection at all; and Homebrew deletes old bottles. Unlike `caddy:2.10` —
an immutable artifact that will still resolve next year — there is no
artifact to name. Renovate can't help either: its `homebrew` manager matches
`^Formula/**.rb` (formula files inside a tap), not Brewfiles.

So the repo's usual discipline — bump a pin in a PR, let the deploy flow
act on it — has nothing to bite on, and the alternative once tried here, a
nightly unattended upgrade job (0.11.0 → 0.18.1), was the
wrong trade for a lone server: it moved every package at once, unreviewed,
and it failed every night regardless, because `.pkg`-based casks
(`dotnet-sdk`) install through sudo and a forced-command SSH session has
no password to give. Versions on the mini now move only when a person
moves them:

```sh
brew bundle install --file ~/.Brewfile --upgrade
```

Bundle rather than plain `brew upgrade`, because only bundle honours
`restart_service: :changed` — `brew upgrade` swaps a binary and leaves the
old process running, which for the Beszel agent means a monitor silently
running stale code. `brew pin <formula>` holds anything that must not move;
pinned packages are skipped.

### Notes

- The install script sets `HOMEBREW_NO_AUTO_UPDATE=1`: the lab is meant to
  keep working with the internet down, and with no scheduled upgrade nothing
  on the mini refreshes Homebrew's metadata except the hand-run upgrade
  above, which does it first.
- Editing the Brewfile: `chezmoi apply` on a laptop installs immediately;
  on a server the merge does, through the `chezmoi` workflow.

## The heartbeat (`tofu/`)

A scheduler's *absence* is the failure nothing inside it can report, so
it is watched from outside the lab: `.forgejo/workflows/heartbeat.yaml`
pings healthchecks.io every 15 minutes from the mini's runner, and
healthchecks.io alerts when the pings stop. Everything else that watches
this lab runs on the same runner and dies with it.

The check is declared, not clicked: `tofu/checks.tf` owns its schedule,
grace period, description and notification channels.

It notifies **Pushover and email**. Pushover is the one that reaches a
phone, landing beside the in-lab alerts from `system/alert-failed` — same
device, same app, while this alert's *origin* stays outside the lab, which
is the whole point of it. Email is the backstop for the case where Pushover
is the thing that's broken. Channels are configured in the healthchecks.io
UI and only *referenced* here, so a data source for a channel that hasn't
been set up will fail the plan.

```sh
cd tofu
op run --env-file=secrets.env -- tofu init
op run --env-file=secrets.env -- tofu plan
op run --env-file=secrets.env -- tofu apply
```

**Why the check lives here** rather than in a `monitoring/` slice:
healthchecks.io is a *provider*, not a capability — the same call already
made for Netlify (see `caddy/tofu/providers.tf`). A check belongs to the
slice that owns the thing being checked, so the tick's check sits beside
the tick. When other flows earn checks, each one goes in its owning slice's
`tofu/`, and none of them collect in a shared root.

**Ping by slug, not UUID.** The flow builds
`https://hc-ping.com/<ping-key>/lab-chezmoi-update` from a project-wide ping
key. So the URL survives the check being destroyed and recreated, one vault
item covers every future check, and the slug sits in the flow next to what
it monitors. The slug is derived by healthchecks.io from the check's `name`,
which is why the name is written slug-shaped — confirm they match after the
first apply.

Two 1Password items, and they are not interchangeable: `healthchecks-api-key`
is the read-write *management* key, used only from this tofu root;
`healthchecks-ping-key` is the far less privileged *ping* key that
`scripts/heartbeat.sh` reads at run time through the runner's service
account. Never put the management key where a job can read it.

## Pushes

`.forgejo/workflows/chezmoi.yaml` runs `chezmoi update` on every server
when a push touches `chezmoi/`. Nothing here needs a schedule: config
changes arrive as pushes, and `create_` files are written on the apply
that first needs them.
