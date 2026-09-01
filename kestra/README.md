# kestra

[Kestra](https://kestra.io) schedules the lab's *jobs* — the chezmoi tick,
the per-service deploy flows, the Obsidian vault syncs, the janitor, the
failure alerter. It is
not a service manager: long-running services are compose slices at the
repo root, each converged by its own `lab.<slice>/deploy` flow chained on the
tick. Kestra replaces the *scheduling* half of launchd, not the
*supervision* half.

Flows are code: every job is one directory — `<slice>/flows/<job>/` at the repo
root, holding `flow.yaml`, with a `script.sh` beside it only when the job
crosses the bridge AND no shared pipeline in `scripts/` covers its shape
— and all of them are
applied with OpenTofu from this slice's `tofu/` root (below), not clicked
into the UI. The UI is for watching runs, reading logs, and re-running
failures.

## The trust model

**Kestra holds the Docker socket and is trusted as root over the lab —
decision 2026-08-31 (Tom's), reversing the original stance.** Both sides
recorded, because the reversal is the interesting part.

The original cap was set when Kestra replaced a couple of launchd
agents: no socket, no host mounts, host work only through the
forced-command bridge — so a compromised Kestra could at worst run
scripts already merged to main. Every step since (the tick, the deploy
fan-out, alerting, OpenTofu CD below) promoted Kestra to being the
platform, and the cap's costs — five lines of SSH boilerplate per flow,
one opaque log blob per job, timeouts that report a stall but cannot end
one — fell on exactly the thing a platform exists to do: start and stop
containers.

What holding the socket means, stated plainly: root over the daemon's
VM, every container, and everything file-shared into it — `/Users` and
`/Volumes`, so all state, the backups, and the op service-account token.
**A Kestra compromise is a lab compromise, vault read included.** The
gate is therefore what gets MERGED (and which images are pinned), not
what Kestra can reach: PR review is the security boundary now. The
consequences run through this file — deploys are native Docker tasks,
the bridge survives as *transport* for work that needs the macOS host
itself, and the forced plugin defaults in `application.yaml` pin the
bridge's connection details instance-wide.

## Why kestra has no deploy flow

Deliberate, and the reason this slice has `scripts/upgrade.sh` instead:
a deploy flow for kestra would have Kestra replace its own executor
mid-execution, killing the very run that triggered it (a sibling task
container would survive the executor's death, but orphaned — nothing
collects the result). The prohibition is *deploy-specific*: this
slice does own its housekeeping flows — `flows/purge/` (the nightly
execution-history janitor), `flows/backup/` (the nightly pg_dump) and
`flows/alert-failed/` (the lab's failure alerter, which is Kestra machinery
rather than a lab workload) — because those run inside Kestra or dump data;
none replaces the executor. Every other
slice's churn can't touch this stack; on the rare occasion kestra itself
changes, you upgrade it by hand:

```sh
kestra/scripts/upgrade.sh [expected-version]
```

Bump the pin in `compose.yaml` via a normal PR first (the tick ships it
here, where it sits inert). The version argument is an *assertion* against
that pin — the script refuses to run if they disagree, so you can't
converge to a version you didn't review or one the tick hasn't landed yet.
It takes a `pg_dump` backup, pulls, converges, and health-polls :8180.
Postgres majors additionally need pg_upgrade or dump/restore.

## How container jobs work

The deploy flows converge compose stacks as native Docker tasks. The
plugin defaults in `application.yaml` give every `shell.Commands` task a
docker CLI image, the socket, the repo checkout — mounted **at its host
path**, so every bind source a compose file names resolves correctly on
the daemon — and `/Volumes` read-only, which is the deploy guard's
window onto mount state. The task container is a sibling of whatever it
converges.

`scripts/deploy.sh` at the repo root is the single shared
implementation: smartening deployment (health gates, pre-pulls,
skip-if-unchanged) is one edit there, not six. Per-slice variation is
*data* — arguments for required drives (jellyfin, qbittorrent), a
`flows/deploy/post.sh` hook for follow-up work (caddy's route reload) —
never a fork of the script.

## How host jobs work

chezmoi, brew, the tar backups and tofu act on the macOS host itself,
which no sibling container can reach. That work leaves through the job
bridge — since the trust decision above, a *transport* rather than a
security boundary, but still the tidiest way for a container to reach
the host:

1. A dedicated ed25519 keypair (the `kestra-job-bridge` 1Password item,
   materialized onto the mini by chezmoi — never in the repo).
2. `~/.ssh/authorized_keys` (chezmoi-managed) binds that key with
   `restrict,command="~/.local/bin/lab-job"` — no pty, no forwarding, and
   sshd runs the dispatcher *instead of* whatever was requested.
3. `lab-job` treats the requested command as a job *name* — exactly two
   `[a-z0-9-]` segments joined by one slash, e.g. `obsidian/main` or
   `dns/plan` — and resolves it in this repo's checkout on the mini (the
   one the tick keeps fresh): the slice's own
   `<slice>/flows/<job>/script.sh` if it exists, else the shared pipeline
   `scripts/<job>.sh`, passed the slice — so `dns/plan` runs
   `scripts/plan.sh dns`, and a slice overrides a pipeline by simply
   having a script of its own.
   The alphabet has no dots, so traversal is unrepresentable. Unknown or
   malformed names exit 64. The dispatcher itself never changes: **adding
   a job = one commit** — one directory (`flow.yaml`, plus a `script.sh`
   only when no pipeline covers the job) in the owning slice.

A leaked webhook key can still only start the tick; what a compromised
Kestra can do is stated honestly in "The trust model" above. The
bridge's connection details (host, user, key) live once, as *forced*
plugin defaults in `application.yaml` — no flow declares them, and no
flow can point the SSH task type anywhere else.

## The flow applier (`tofu/`)

Kestra's API resources as code, the same way `forgejo/tofu` manages
repositories: every `<slice>/flows/<job>/flow.yaml` in the repo becomes a
managed `kestra_flow`. State lives in Garage
(`s3://tofu-state/kestra/terraform.tfstate`). Flows edited in the Kestra
UI are reverted by the next apply; the repo is the source of truth.

```sh
cd kestra/tofu
op run --env-file=secrets.env -- tofu init
op run --env-file=secrets.env -- tofu plan
op run --env-file=secrets.env -- tofu apply
```

Day to day nobody types that: `lab.kestra/apply` runs it on every green
tick (see "OpenTofu CD" below). The commands remain the bootstrap path —
the first apply is what registers the flow that takes over.

**Adding a job** = one commit adding one directory to the owning slice:
`flows/<name>/flow.yaml` (+ `flows/<name>/script.sh` if it touches the mini) —
`flows.tf` picks the YAML up automatically, and lab-job resolves
`<slice>/<name>` to the script from the repo checkout.

### Naming: one shape in three places

A flow's identity is `<namespace>/<id>`, and both halves fall out of where
the flow lives:

```
forgejo/flows/deploy/flow.yaml   the path
lab.forgejo / deploy             the flow — namespace / id
forgejo/deploy                   the lab-job name the flow invokes
```

One string, three contexts: `<slice>` becomes `lab.<slice>`, the job
directory becomes the flow id. Nothing to decide when you add the thirtieth
flow.

Namespaces stay FLAT — `lab.<slice>`, never `lab.services.forgejo`. A
deeper tree forces a taxonomy call per slice (is caddy infrastructure or a
service? is obsidian automation?) that this repo has refused to make since
the 0.6.0 restructure; the flat form is derivable and needs no judgement.

The YAML remains the source of truth: `flows.tf` reads `id` and `namespace`
out of the file rather than inferring them from the path, so a rename shows
up in the plan. The convention above is a convention — nothing enforces it.

The cross-cutting axis is **labels**, not more namespace:

| Label | Values | Purpose |
| --- | --- | --- |
| `job` | `deploy`, `backup`, `sync`, `cert`, `tick`, `maintenance`, `heartbeat`, `health`, `plan`, `apply` | filtering flows and executions in the UI |
| `alert` | `high`, `low` | severity routing in `system/alert-failed` |

So "show me every backup" is a label filter, and you never have to choose
between organising by slice and organising by function.

## Trigger flows

Two flows carry the lab's scheduling semantics (2026-09-01):

- **`lab.kestra/tick`** — the 15-minute pulse, a pure no-op. Chaining on
  it says "run regularly".
- **`lab.kestra/push-to-main`** — Forgejo's push webhook lands here (the
  poke; `forgejo/tofu/webhook.tf` derives the URL from its flow file),
  and it converges the checkout (a subflow of `lab.chezmoi/update`)
  before going green. Chaining on it says "run when main moves" — and
  its SUCCESS *means* the push has landed on the mini, which is exactly
  the guarantee the apply chain consumes. Not a pure no-op, on purpose:
  the alternative was `apply.sh` racing the update flow with its own
  `git pull`, freshness enforced in the wrong layer.

Before the split, everything periodic chained on `lab.chezmoi/update` by
convenience, so "needs the converged checkout" and "wants a timer" were
indistinguishable. Now a chain is a dependency claim: deploys and
packages chain on update because they consume what it converges; update
runs on the tick and inside push-to-main; the apply chain hangs off
push-to-main because applies are push-shaped.

## OpenTofu CD

Forgejo issue #9's answer — reworked 2026-09-01 after the 1Password
rate-limit outage (CHANGELOG 0.16.0). The original design planned every
root and auto-applied kestra's on every tick: twelve `op run`
invocations per 15 minutes, which exhausted the service account's daily
quota on its very first full day. CD now runs on pushes plus one daily
report, and each pipeline makes exactly one op invocation.

- **`lab.<slice>/apply` — automatic, on push to main.**
  `lab.kestra/apply` chains on push-to-main and runs FIRST, so flow
  changes register before the other roots apply behind it (they chain on
  its SUCCESS). Freshness is inherited, not fetched: push-to-main only
  goes green after converging the checkout, so by the time any apply
  runs, the push that fired it has landed on the mini.

  **This reverses the 2026-08-31 human-gate decision — Tom's call,
  2026-09-01, both sides recorded.** The gate existed because unofficial
  providers have real WRITE bugs (svalabs' full-object PATCH once
  blanked wiki branches on an UPDATE). What stands in for the human now:
  `prevent_destroy` on the resources that matter — Forgejo repositories,
  every DNS record, the front-door wildcards — makes a destroying plan
  FAIL the apply loudly; deleting one deliberately means removing its
  guard in the same diff, and that pairing is the confirmation step.
  Stated honestly: the guard covers destroys only. The update-bug class
  it cannot catch is accepted risk, with the nightly backups as the
  recovery — and PR review remains the actual gate, since a PR can
  remove a guard. `kestra_flow` resources are deliberately unguarded:
  destroy+create is their routine rename mechanic.
- **`lab.<slice>/plan` — the daily drift report**, staggered 07:00–07:40.
  Exit 2 (drift) lands as WARNING — a quiet morning nag until a push or
  a manual apply converges it. Exit 1 (the plan itself broke) goes RED
  via a gate task on the exit code: during the outage, "1Password is
  down" rendered as drift for hours, and a plan that cannot run is an
  outage, not a nag.
- **`garage/tofu` stays outside all of this**, manual by design: its
  disposable state seeds the very backend the other roots stand on.

The host half is the shared pipelines `scripts/plan.sh` and
`scripts/apply.sh` — deploy.sh's pattern on the bridge side, reached by
lab-job's pipeline fallback, so no per-slice script exists. ONE `op run`
per pipeline, deliberately: every invocation spends the service
account's metered quota, and that budget is what ran out on 2026-09-01.
Repo-versioned, so merges ship changes — nothing to chezmoi-apply.

What namespaces do *not* buy here: namespace-level secrets, variables,
plugin defaults and RBAC are all Enterprise features. `secret('LAB_SSH_KEY')`
resolves from an instance-wide `SECRET_*` environment variable whatever the
namespace. What they do buy is UI grouping and the prefix match below.

## Timeouts

Every flow has one. A hang is worse than a failure: it produces no failure
state, so nothing in "Alerting" below can see it — the task just shows as
running, whatever chains off it never fires, and `concurrency: QUEUE` stacks
another execution every interval behind it. A timed-out task FAILS, which
turns an invisible hang into a notification. (Cost us a 10-minute silent
stall on 2026-08-28, when `chezmoi` blocked on a prompt.)

| Flow | Timeout | Bound by |
| --- | --- | --- |
| `lab.chezmoi/update` | PT10M | must sit under the 15-minute tick interval |
| `lab.obsidian/main`, `/dnd` | PT8M | must sit under their 10-minute schedule |
| backups | PT15M | ~45x the slowest observed run (forgejo, ~20s) |
| deploys | PT20M | a cold image pull is the unbounded case |
| `lab.caddy/renew-certs` | PT15M | lego waits 90s for DNS propagation first |
| `lab.kestra/purge` | PT30M | a first purge after a backlog deletes a lot |
| `lab.<slice>/plan` | PT10M | a first run downloads providers; after that, seconds |
| `lab.<slice>/apply` | PT30M | provider write paths can be slow; a hung apply must still die |
| `lab.kestra/tick`, `/push-to-main` | PT1M | no-op Return tasks; a hang here means Kestra itself is sick |
| `lab.chezmoi/packages` | PT20M | a cold cask or mas download; a satisfied run is ~1s |
| `lab.chezmoi/packages-upgrade` | PT45M | deliberately generous — see below |
| `lab.chezmoi/heartbeat`, `system/alert-failed` | PT1M | one HTTP call each |

**A timeout on an SSH task reports a stall; it does not abort one.** Killing
a Kestra execution kills the SSH client, not the process on the mini — so a
timed-out backup keeps running host-side, and it is the script's own `EXIT`
trap, not Kestra, that restarts a stopped stack. If you need the host
process dead, `pkill` it on the mini. This is why the backup ceilings are
set wide rather than tight: firing one early gains nothing and costs a
false page. (Docker tasks — the deploys — do not share this wart: killing
the execution kills the task container, process and all. One of the
trust model's quieter wins.)

## Alerting

Two mechanisms, layered deliberately, because they fail in different ways.

**Inside the lab — `system/alert-failed`.** One flow, triggered by
`NAMESPACE STARTS_WITH "lab."` and `STATE IN [FAILED, WARNING]`. Every flow
in every slice is covered, including slices that don't exist yet: that
prefix match is the entire payoff of hierarchical namespaces. Severity
comes from each flow's `alert:` label AND the state (2026-09-01): only
FAILED + `high` becomes Pushover priority 1 (bypasses quiet hours);
everything else is -1 (tray, no buzz). So a failed backup wakes you, a
failed deploy doesn't, and a WARNING never does — WARNING means
"degraded but understood" (a drift report), which is why the plan flows
carry `alert: high` and still nag quietly. The rate-limit outage is the
cautionary tale: erroring plans rendered as WARNINGs and hid for hours.
The flow's own header explains why it sits in `system` and what that
costs.

**Outside the lab — the heartbeat.** `lab.chezmoi/update` pings
healthchecks.io as its final task. Everything above runs inside Kestra and
therefore shares Kestra's fate: container down, postgres wedged, mini
powered off, and silence looks exactly like health. The ping stopping is the
only signal that leaves the building. The check is declared in
`chezmoi/tofu/` (schedule, grace, channels) — it belongs to the slice that
owns the tick, not to this one. It is a *separate flow*
(`lab.chezmoi/heartbeat`) chained on the tick's SUCCESS, not a task on the
tick — a watchdog must not be able to break what it watches. As a task, an
unresolvable secret or a healthchecks.io outage turns the tick red, and a red
tick chains to nothing, so every deploy stops. (`allowFailed` does not cover
this: it tolerates HTTP status >= 400 only, not connection errors and not a
secret that fails to resolve.)

**Verify both after the first apply.** A Flow trigger that matches nothing
does not error — it silently never fires, which is indistinguishable from a
healthy lab. Re-run this whenever the trigger or the label scheme changes.

*Passed 2026-08-28* against Kestra v1.3.34: the trigger fires, it does NOT
batch (two failures produced two notifications), and an unlabelled flow
alerts at low priority rather than failing the alerter. The throwaway test
flow is below.


1. Fail any `lab.*` flow on purpose (point a deploy at a bad job name, or
   kill a running execution). A notification should arrive in seconds.
2. Do it **twice**. Two failures must produce two notifications. One
   notification means the precondition is batching over its default time
   window — set an explicit short `timeWindow`, or fall back to the older
   per-execution `conditions:` form.
3. Pause the tick for longer than the grace period and confirm
   healthchecks.io actually notifies you.

The throwaway used for 1 and 2 — create it in the UI, not the repo, so it
touches nothing real; delete it afterwards, since tofu won't manage it:

```yaml
id: alert-test
namespace: lab.test
labels:
  alert: high
tasks:
  - id: boom
    type: io.kestra.plugin.core.execution.Fail
    errorMessage: Testing system/alert-failed
```

## Secrets: 1Password is the origin

No secret is generated on the machine. You create them in the vault;
chezmoi `create_` templates (`chezmoi/home/Docker/kestra/`) materialize
them into files under `~/Docker/kestra/`. The templates are only evaluated
while a file is MISSING — `op` and the internet are bootstrap dependencies,
not tick dependencies.

> **Adding a secret to a `create_` template does nothing on a machine that
> already has the file.** `chezmoi apply` will not re-render it, and there is
> no warning: the new `SECRET_*` line simply never reaches the container, and
> the first flow to call `secret()` for it fails at runtime. To roll one out:
> delete `~/Docker/kestra/kestra.env` on the mini, `chezmoi update` (which
> needs `OP_SERVICE_ACCOUNT_TOKEN` — see below), then recreate the container
> so it re-reads the env file. Kestra reads `SECRET_*` at boot only.
>
> Interactive `chezmoi` on the mini needs the service-account token in the
> environment. `.zprofile` exports it on servers, so a login shell is fine;
> if you're in something that didn't source it:
> `export OP_SERVICE_ACCOUNT_TOKEN="$(cat ~/Docker/1password/service-account-token)"` Wipe the directory and `chezmoi apply` and the
*same* secrets come back — recreating infrastructure can never lock you
out, and there's nothing to copy back into the vault afterwards.

Items (Wolfe.Lab vault; generate values in 1Password, letters+digits to
keep env files quote-free):

| Item | Fields | Notes |
| --- | --- | --- |
| `kestra-admin` | Login item: `username` = trwolfe13@gmail.com, `password` | UI login; also used by this slice's `tofu/` root |
| `kestra-postgres` | Password item: `password` | kestra ↔ postgres, never typed by a human |
| `kestra-encryption-key` | API Credential item: `credential` | **exactly 32 chars** |
| `kestra-job-bridge` | SSH Key item, ed25519 | 1Password generates it |
| `pushover` | API Credential item: `credential` = application token, `username` = user key | the lab's only notification transport |
| `healthchecks-ping-key` | API Credential item: `credential` = the project ping key | dead man's switch; one key covers every check |

## First-time setup (on the mini, GUI session — `op` needs it)

1. Create the six 1Password items above.
2. `chezmoi apply` — materializes `~/Docker/kestra/` (env files, SSH
   keypair) and `~/.docker-headless/config.json` via `create_` templates.
3. `chezmoi apply` **again** — authorized_keys templates the public key in
   (it didn't exist during the first render).
4. `./setup.sh` from the repo root brings every stack up (Kestra can't
   deploy itself into existence — this is the one manual bring-up).
5. Log in at http://macmini.local:8180 with `kestra-admin` and apply the
   flows: see "The flow applier" above.

## Cutover from launchd (done)

The `md.obsidian.headless-sync-*` launch agents are gone — replaced by the
`obsidian` automation's one-shot passes every 10 minutes. The pattern,
for the next launchd retirement: delete the plist from source, add the
target to `chezmoi/home/.chezmoiremove` (deleting a source file only makes chezmoi
*stop managing* the target), and `launchctl bootout` the loaded agent on
the mini.

## Backup

Everything stateful is under `~/Docker/kestra/`. `flows/backup/script.sh` dumps
the database to a dated file in `/Volumes/Data2/backups/kestra/` (refusing
to run if the drive isn't mounted — which also gates upgrades: no drive, no
pre-upgrade dump, no upgrade) — run it by
hand, or through the bridge as `kestra/backup`; `scripts/upgrade.sh` calls
it (labelled `pre-<version>`) before every upgrade, and the `lab.kestra/backup`
flow runs it nightly at 03:20 — live, no downtime, which is what makes a
backup flow safe here where a deploy flow isn't: a dump never restarts the
executor. Retention: the last 10 scheduled dumps are kept; the labelled
pre-upgrade dumps are never pruned. Cold backup of the whole
state: stop the stack, copy the directory. The flows themselves need no
backup — they're in this repo.
