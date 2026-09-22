# build

The lab's jobs as a CLI, `lab`, built on [Ritten](https://github.com/ritten-org/Ritten):
`Wolfe.Lab.Build` in `src/`, its tests in `tests/`. A workflow file is a
trigger and one command; what the command does lives here, in C#, where
it can be typed, rehearsed and tested.

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

A job's name has to read from inside the slice it runs in, because that
is all the context there is. `verify` says enough in `restic/`, where
the repositories are the only thing it could mean; in `forgejo/` it says
nothing at all, which is why the slice-level check carries its subject
and is called `restore-drill`. One intent gets one verb, too: making a
node match its slice is `deploy` whether the stack is containers, a
supervised agent, or both.

A workflow of its own is for what only that slice does. `caddy` is the
deploy every compose slice runs plus the two steps that belong to the
front door: gathering every slice's `caddy.caddyfile` and reloading the
running container. That gather is the one step that reads other slices,
and it reads the CHECKOUT rather than the install root on purpose — a
route added in the same push as its slice would otherwise depend on
which of the two workflows the runner reached first.

A slice is a unit of deployment, not necessarily a container. The
`agents` workflow is for the ones whose stack is a supervised host
process instead — a model server needing a GPU no container on macOS can
reach. Such a slice declares what it wants running under `agents` in its
`ritten.json` (the program, its arguments and environment, where its log
goes) and carries no compose file at all.

A root module is a component of its slice. `caddy/tofu/` carries a
`ritten.json` of its own naming the `tofu` workflow, because a slice
declares one workflow and the slice's is the stack's. `check` plans the
root on the pull request — formatting first, then a plan whose diff lands
in the run's report — and `deploy` plans again on the merge and applies
only what that plan found. Every root runs under the same state backend,
declared once in `build/tofu-state.env` and found by walking up from the
root, plus its own `secrets.env`; both name secrets by reference, read
through Ritten's provider as each command starts.

A job can be shared. `DeployJob<TSettings>` installs a slice and
converges its compose stack for any slice that has one. Every compose
slice runs it, on either node: the Pi gets the SDK from its own chezmoi
profile, so there is no longer a machine where the lab's jobs cannot
run, and the shell script this replaced is gone.
`BackupJob<TSettings>`, `RestoreJob<TSettings>` and `DrillJob<TSettings>`
are the backup, restore and restore-drill pipelines for any slice whose
`ritten.json` carries a `backup` section
(paths, excludes, the container to stop — or none for a warm snapshot —
and the `verify` paths a restore must bring back). A workflow offers
them by listing them beside its own jobs, and its settings record says
so by carrying the section (`IBackupSettings`), so the section exists
only where the jobs that read it are offered. A job reads its own
slice's declaration and no other's.

## Layout

A folder under `src/Wolfe.Lab.Build` is one of four things, and the
tree says which:

- `Workflows/<name>/` — one per `"workflow"` a `ritten.json` can name:
  the workflow class, its settings record, and the jobs and steps only
  it lists. Where several names are one family (`docker`, `image`,
  `dotnet-service`) they share a folder.
- `Deploy/`, `Backup/`, `Agents/`, `Gates/` — pipelines several
  workflows list: a shared job or step and what it consumes. A client
  that only one pipeline uses (the rsync installer, the state
  directories, the launchd supervisor) lives with that pipeline.
- `Clients/<name>/` — clients more than one place reaches through:
  the interface, the real one, its dry-run twin and the `AddX()` that
  registers the pair. `Secrets`, `Restic`, `Heartbeat`, `Alerts`.
- `Slices/` and `Values/` — what a slice is (its directories, its
  declared volumes, the steps every job opens with) and the value types
  the rest is typed in. `LabJob` and `LabRuntime` sit at the root.

Dependencies point one way: workflows use pipelines, pipelines use
clients, everything uses slices and values. Nothing under `Clients/`
knows a workflow exists.

The other line is between this project and Ritten. A client for a tool
with no lab policy in it — Docker, git, the .NET SDK, OpenTofu — is a
Ritten package, consumed as one. A step copied from Ritten is owned here
only when it has been changed for the lab (the compose steps, which take
the slice's release and resolved secrets); a copy that would be
identical is not a copy, it is a `using`.

## Clients

Ritten's own where they exist — `IGit` for every git operation, its
command runner for every process. The lab adds what Ritten doesn't have:

- **Secrets** — Ritten's `ISecretProvider`, with `Ritten.OnePassword`
  registered once in `Program.cs` against the node's service-account
  token file. A setting names a secret; a step resolves it at the moment
  of use, so a job that has nothing to push never touches the vault.
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
which CI on the pull request is there to catch first.

## How workflows run it

Today: `dotnet run --project ../build/src/Wolfe.Lab.Build -- <job>`
from the slice's directory, on a node with the .NET SDK (the Brewfile's
`dotnet-sdk` cask; the runner's PATH includes it). Each run compiles
from the checkout, which is seconds on the mini and needs no install.
Packing it as a tool and installing that on every node through chezmoi
is the next step; then the short-cycle workflows need no checkout at all.

CI builds, format-checks and tests this solution in the .NET SDK image
on the containerised runner (`build.yaml`), so a change that would fail on
the mini fails on the pull request.
