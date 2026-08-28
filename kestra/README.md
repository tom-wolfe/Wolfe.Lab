# kestra

[Kestra](https://kestra.io) schedules the lab's *jobs* — the chezmoi tick,
the per-service deploy flows, the Obsidian vault syncs, the janitor, the
failure alerter. It is
not a service manager: long-running services are compose slices at the
repo root, each converged by its own `lab.<slice>/deploy` flow chained on the
tick. Kestra replaces the *scheduling* half of launchd, not the
*supervision* half.

Flows are code: every job is one directory — `<slice>/flows/<job>/` at the repo
root, holding `flow.yaml` with its `script.sh` beside it — and all of them are
applied with OpenTofu from this slice's `tofu/` root (below), not clicked
into the UI. The UI is for watching runs, reading logs, and re-running
failures.

## Why kestra has no deploy flow

Deliberate, and the reason this slice has `scripts/upgrade.sh` instead:
a deploy flow for kestra would have Kestra replace its own executor
mid-execution — the SSH task's client dies with the container, killing the
very run that triggered it. The prohibition is *deploy-specific*: this
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

## How host jobs work

Kestra runs in a container and deliberately cannot touch the host — no
Docker socket, no host mounts beyond its own state. Host-side work leaves
through one audited door:

1. A dedicated ed25519 keypair (the `kestra-job-bridge` 1Password item,
   materialized onto the mini by chezmoi — never in the repo).
2. `~/.ssh/authorized_keys` (chezmoi-managed) binds that key with
   `restrict,command="~/.local/bin/lab-job"` — no pty, no forwarding, and
   sshd runs the dispatcher *instead of* whatever was requested.
3. `lab-job` treats the requested command as a job *name* — exactly two
   `[a-z0-9-]` segments joined by one slash, e.g. `forgejo/deploy` or
   `obsidian/main` — and resolves it to
   `<slice>/flows/<job>/script.sh` in this repo's checkout on the mini
   (the one the tick keeps fresh).
   The alphabet has no dots, so traversal is unrepresentable. Unknown or
   malformed names exit 64. The dispatcher itself never changes: **adding a
   job = one commit** adding one directory (`flows/<job>/` — `script.sh` plus its
   `flow.yaml`) to the owning slice.

So a compromised Kestra (or a leaked webhook key) can at worst run scripts
that were merged to main, never arbitrary commands.

## The flow applier (`tofu/`)

Kestra's API resources as code, the same way `forgejo/tofu` manages
repositories: every `<slice>/flows/<job>/flow.yaml` in the repo becomes a
managed `kestra_flow`. State lives in Garage
(`s3://tofu-state/kestra/terraform.tfstate`). Flows edited in the Kestra
UI are reverted by the next apply; the repo is the source of truth.

```sh
cd tofu
op run --env-file=secrets.env -- tofu init
op run --env-file=secrets.env -- tofu plan
op run --env-file=secrets.env -- tofu apply
```

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
| `job` | `deploy`, `backup`, `sync`, `cert`, `tick`, `maintenance` | filtering flows and executions in the UI |
| `alert` | `high`, `low` | severity routing in `system/alert-failed` |

So "show me every backup" is a label filter, and you never have to choose
between organising by slice and organising by function.

What namespaces do *not* buy here: namespace-level secrets, variables,
plugin defaults and RBAC are all Enterprise features. `secret('LAB_SSH_KEY')`
resolves from an instance-wide `SECRET_*` environment variable whatever the
namespace. What they do buy is UI grouping and the prefix match below.

## Alerting

Two mechanisms, layered deliberately, because they fail in different ways.

**Inside the lab — `system/alert-failed`.** One flow, triggered by
`NAMESPACE STARTS_WITH "lab."` and `STATE IN [FAILED, WARNING]`. Every flow
in every slice is covered, including slices that don't exist yet: that
prefix match is the entire payoff of hierarchical namespaces. Severity
comes from each flow's `alert:` label — high becomes Pushover priority 1
(bypasses quiet hours), low becomes -1 (tray, no buzz) — so a failed backup
wakes you and a failed deploy doesn't. The flow's own header explains why
it sits in `system` and what that costs.

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
healthy lab:

1. Fail any `lab.*` flow on purpose (point a deploy at a bad job name, or
   kill a running execution). A notification should arrive in seconds.
2. Do it **twice**. Two failures must produce two notifications. One
   notification means the precondition is batching over its default time
   window — set an explicit short `timeWindow`, or fall back to the older
   per-execution `conditions:` form.
3. Pause the tick for longer than the grace period and confirm
   healthchecks.io actually emails you.

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
the database to a dated file in `/Volumes/Data1/backups/kestra/` (refusing
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
