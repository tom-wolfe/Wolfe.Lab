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
`import`, `backup`, `restore`, `verify`), `files` (`backup`, `restore`,
`verify`), `restic` (`offsite`, `verify`), `service` (`backup`, `restore`
and `verify`, for the compose slices the shell still deploys), `agents`
(`converge`).

A slice is a unit of deployment, not necessarily a container. The
`agents` workflow is for the ones whose stack is a supervised host
process instead — a model server needing a GPU no container on macOS can
reach. Such a slice declares what it wants running under `agents` in its
`ritten.json` (the program, its arguments and environment, where its log
goes) and carries no compose file at all.

A job can be shared: `BackupJob<TSettings>`, `RestoreJob<TSettings>` and
`VerifyJob<TSettings>` are the backup, restore and restore-drill
pipelines for any slice whose `ritten.json` carries a `backup` section
(paths, excludes, the container to stop — or none for a warm snapshot —
and the `verify` paths a restore must bring back), and a workflow offers
them by listing them beside its own jobs. A job reads its own slice's
declaration and no other's.

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
- **Agents** — `IServiceSupervisor` renders a slice's declared agents to
  the platform's units and converges the supervisor onto them: launchd
  today, systemd user units when the primary node is a Linux box. The
  declaration is platform-neutral, so that day changes one registration
  and no slice. A converge compares the whole rendered unit and restarts
  only on a difference; the executable's own timestamp is part of that
  text, so upgrading the binary counts as a change to the agent without
  anyone having edited the declaration. An agent whose program is not on
  the node is refused rather than installed to fail. The rehearsal reads
  the node — which units are installed, what the supervisor is running —
  and writes nothing.

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
