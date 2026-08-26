# kestra

[Kestra](https://kestra.io) schedules the lab's *jobs* — the chezmoi tick,
the per-service deploy flows, the Obsidian vault syncs, the janitor. It is
not a service manager: long-running services are compose slices at the
repo root, each converged by its own `deploy-<svc>` flow chained on the
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
slice does own its housekeeping flows (`flows/purge/flow.yaml`, the nightly
execution-history janitor; a scheduled backup later), because those run
inside Kestra or dump data — neither replaces the executor. Every other
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

1. A dedicated ed25519 keypair (the `Kestra Job Bridge` 1Password item,
   materialized onto the mini by the bootstrap script — never in the repo).
2. `~/.ssh/authorized_keys` (chezmoi-managed) binds that key with
   `restrict,command="~/.local/bin/lab-job"` — no pty, no forwarding, and
   sshd runs the dispatcher *instead of* whatever was requested.
3. `lab-job` treats the requested command as a job *name* — exactly two
   `[a-z0-9-]` segments joined by one slash, e.g. `forgejo/deploy` or
   `obsidian-sync/main` — and resolves it to
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

## Secrets: 1Password is the origin

No secret is generated on the machine. You create them in the vault; the
bootstrap script materializes them into env files under `~/Docker/kestra/`
with `op read`. Wipe that directory and re-bootstrap and the *same* secrets
come back — recreating infrastructure can never lock you out, and there's
nothing to copy back into the vault afterwards.

Items (Wolfe.Lab vault — API Credential type unless noted; generate values
in 1Password, letters+digits to keep env files quote-free):

| Item | Fields | Notes |
| --- | --- | --- |
| `Kestra Admin` | `username` = trwolfe13@gmail.com, `credential` | UI login; also used by this slice's `tofu/` root |
| `Kestra Postgres` | `credential` | kestra ↔ postgres, never typed by a human |
| `Kestra Encryption Key` | `credential` | **exactly 32 chars** |
| `Kestra Job Bridge` | SSH Key item, ed25519 | 1Password generates it |

## First-time setup (on the mini, GUI session — `op` needs it)

1. Create the four 1Password items above.
2. `chezmoi apply` — bootstrap materializes `~/Docker/kestra/` (env files,
   SSH keypair) and `~/.docker-headless/config.json`.
3. `chezmoi apply` **again** — authorized_keys templates the public key in
   (it didn't exist during the first render).
4. `./setup.sh` from the repo root brings every stack up (Kestra can't
   deploy itself into existence — this is the one manual bring-up).
5. Log in at http://macmini.local:8180 with `Kestra Admin` and apply the
   flows: see "The flow applier" above.

## Cutover from launchd (done)

The `md.obsidian.headless-sync-*` launch agents are gone — replaced by the
`obsidian-sync` automation's one-shot passes every 10 minutes. The pattern,
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
it (labelled `pre-<version>`) before every upgrade, and the `backup-kestra`
flow runs it nightly at 03:20 — live, no downtime, which is what makes a
backup flow safe here where a deploy flow isn't: a dump never restarts the
executor. Retention: the last 10 scheduled dumps are kept; the labelled
pre-upgrade dumps are never pruned. Cold backup of the whole
state: stop the stack, copy the directory. The flows themselves need no
backup — they're in this repo.
