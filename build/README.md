# build

The lab's jobs as a CLI, `lab`, built on [Ritten](https://github.com/ritten-org/Ritten):
`Wolfe.Lab.Build` in `src/`, its tests in `tests/`. A workflow file is a
trigger and one command; what the command does lives here, in C#, where
it can be typed, rehearsed and tested. This is where the shell scripts
in `scripts/` and each slice's `flows/` are going, one job at a time.

## How a slice opts in

A slice that the CLI serves carries a `ritten.json` naming its workflow
— `"workflow": "obsidian"` — plus that workflow's settings. Run from the
slice's directory, `lab` offers exactly that workflow's jobs as
commands, each option of which is a job argument the job declared. A
workflow is a class here: its jobs, each job's ordered steps, and the
settings shape its `ritten.json` must satisfy, judged before anything
runs. Steps hand each other typed values (a `Vault`, say) rather than
sharing state, and reach outside the working directory only through a
client that has a dry-run twin, so `--dry-run` rehearses any job
without a side effect.

Workflows so far: `obsidian` (`sync --vault <name>`), `immich` (`deploy`,
`import`), `files` (`backup`), `restic` (`offsite`, `verify`).

A job can be shared: `BackupJob<TSettings>` is the backup pipeline for
any slice whose `ritten.json` carries a `backup` section (paths,
excludes, and the container to stop — or none for a warm snapshot), and
a workflow offers it by listing it beside its own jobs.

## Clients

Ritten's own where they exist — `IGit` for every git operation, its
command runner for every process. The lab adds what Ritten doesn't have:

- **Secrets** — `ISecrets` reads one `op://` reference through the
  1Password CLI, authenticated the way `scripts/secrets.sh` is. A
  setting names a secret; a step reads it at the moment of use, so a
  job that has nothing to push never touches the vault.
- **Obsidian** — `IObsidian` runs the headless client through the
  `nvm-run` wrapper chezmoi installs.
- **Restic** — `IRestic` runs the binary with the repository in its
  environment, never on its command line. The rehearsal is restic's own
  `--dry-run` where it has one (backup, forget), so a dry run lists what
  a snapshot would add or retention would drop; a copy is skipped and
  a check keeps to structure.
- **Heartbeat** — `IHeartbeat` pings a healthchecks.io check, the dead
  man's switch a scheduled job reports to as its last step. The
  rehearsal never pings: a switch told the job ran is worse than none.

## Alerting

A job that fails on a runner pages, the way every workflow's
`if: failure()` step used to: the CLI detects the runner from the
environment (Ritten's `ForgejoActionsRuntime`, which the lab's
`LabRuntime` extends) and that runtime contributes a result sink which posts to
Pushover when the run did not succeed — job, failed step, first error,
and the run's page. At a terminal there is no runtime and no page.
What the sink cannot cover is a run that never reaches the CLI: a
`dotnet run` that fails to restore or compile exits red without paging,
which CI on the pull request is there to catch first. The workflows
still on `scripts/` keep `scripts/alert.sh`.

## How workflows run it

Today: `dotnet run --project ../build/src/Wolfe.Lab.Build -- <job>`
from the slice's directory, on a node with the .NET SDK (the Brewfile's
`dotnet-sdk` cask; the runner's PATH includes it). Each run compiles
from the checkout, which is seconds on the mini and needs no install.
Packing it as a tool and installing that on every node through chezmoi
is the next step; then the short-cycle workflows need no checkout at all.

CI builds, format-checks and tests this solution in the .NET SDK image
on the containerised runner (`ci.yaml`), so a change that would fail on
the mini fails on the pull request.
