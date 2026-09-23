# build

The lab's jobs as a CLI, `lab`, built on [Ritten](https://github.com/ritten-org/Ritten):
`Wolfe.Lab.Build` in `src/`, its tests in `tests/`. A workflow file is a
trigger and one command; what the command does lives here, in C#, where
it can be typed, rehearsed and tested.

## How a component opts in

A directory the CLI serves carries a `ritten.json` naming its workflow
— `"workflow": "docker"` — plus that workflow's settings. That directory
is a *component*: a slice is the folder that groups a service's
components (`sonarr/compose/`, `sonarr/backup/`, `forgejo/tofu/`,
`forgejo/runners/`), and never carries a declaration of its own. Run
from a component's directory, `lab` offers exactly that workflow's jobs
as commands, each option of which is a job argument the job declared. A
workflow is a class here: its jobs, each job's ordered steps, and the
settings shape its `ritten.json` must satisfy, judged before anything
runs. Steps hand each other typed values (a `Release`, say) rather than
sharing state, and reach outside the working directory only through a
client that has a dry-run twin, so `--dry-run` rehearses any job
without a side effect.

A workflow is a *shape* of component, not a service. The regular shapes
carry most of the lab: `docker` (a compose stack — check on the pull
request, deploy on the merge), `tofu` (a root module — plan on the pull
request, apply on the merge), `backup` (what a snapshot holds, what has
to be quiet while it is taken, and what a restore must bring back),
`image` and `dotnet-service`. Two slices with the same shape share a
workflow and differ only in what they declare. What only one slice does
is a component of its own with a workflow of its own — `caddy/certs`,
`caddy/routes`, `forgejo/runners`, `garage/layout`, `immich/import`,
`gatus/health` — named for the slice and the thing, so a `ritten.json`
reads as what it is. Nothing is shared *across* workflows: a step two
workflows need belongs to a domain module under `Clients/`, and each
workflow lists it for itself.

A job's name has to read from inside the component it runs in, because
that is all the context there is: `renew` in `caddy/certs/`, `register`
in `forgejo/runners/`, `init` in `garage/layout/`, `verify` in
`restic/repositories/`. One intent gets one verb, too: making a node
match its component is `deploy` whether the stack is containers, a
supervised agent, or a root module.

A compose component is installed, not run from the checkout. A runner
checks the repository out into a disposable workspace, and a container
bind-mounts files — config directories, a Caddyfile, route snippets —
that it goes on reading after the job that started it is gone. So every
docker component names a `release`, is rsync'd under that name into one
flat root (`~/.local/share/Wolfe.Lab/<release>`), and is converged from
there with its `secrets.env` resolved into the environment of that one
`compose up`. Ritten's own compose steps read the component in the
checkout, which is right for the check and wrong for the deploy; the
one step that differs is owned here, the rest are a `using`.

`caddy/routes` is the one component that reads other components, and
deliberately: it gathers every `caddy.caddyfile` in the checkout into a
release of its own that the door's Caddyfile imports, so adding a
service never edits the front door, and a route added in the same push
as its stack does not depend on which workflow the runner reached first.

A backup component snapshots a release's state. Its `ritten.json` names
the release (the snapshot's tag and, when a container is named in
`stop`, the installed stack that is stopped for the duration), the
paths, the excludes, and the `verify` paths a restore must bring back.
The repositories themselves are the restic slice's to define: every
backup component reads `restic/restic.env` (or `sftp.env` on a Linux
node) by walking up to the checkout — the one file a component reads
outside its own directory, because a copy in every component would be a
copy that drifts.

A tofu root runs under the same state backend, declared once in
`build/tofu-state.env` and found by walking up from the root, plus its
own `secrets.env`; both name secrets by reference, read through Ritten's
provider as each command starts.

## Layout

A folder under `src/Wolfe.Lab.Build` is one of three things, and the
tree says which:

- `Workflows/<name>/` — one per `"workflow"` a `ritten.json` can name,
  the folder named for the workflow (`CaddyRoutes/` for
  `caddy-routes`): the workflow class, its settings record under
  `Models/`, and the jobs and steps only it lists under `Jobs/` and
  `Steps/`. Where several names are one family (`docker`, `image`,
  `dotnet-service`) they share a folder.
- `Clients/<domain>/` — a domain module: what more than one workflow
  reaches through. A client, its dry-run twin and the `AddX()` that
  registers the pair, and under `Steps/` the steps that consume it.
  `Releases` (the installer and where a component is released to),
  `Volumes` (the mounted-drive guard), `Gates`, `Restic`, `Secrets`,
  `Heartbeat`, `Alerts`, `Agents`, `Caddy`, and the rest.
- `Values/` — the value types the rest is typed in (`HostPath`,
  `ServiceUrl`). `LabJob`, `LabRuntime` and `Program` sit at the root.

Dependencies point one way: workflows use domain modules, everything
uses values. Nothing under `Clients/` knows a workflow exists, and no
workflow knows another.

The other line is between this project and Ritten. A client for a tool
with no lab policy in it — Docker, git, the .NET SDK, OpenTofu — is a
Ritten package, consumed as one, and so are its steps. A step copied
from Ritten is owned here only when it has been changed for the lab
(`ConvergeRelease`, which converges from the release with the resolved
secrets); a copy that would be identical is not a copy, it is a `using`.

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
- **Agents** — `IServiceSupervisor` renders a component's declared agents to
  the platform's units and converges the supervisor onto them: launchd
  today, systemd user units when the primary node is a Linux box. The
  declaration is platform-neutral, so that day changes one registration
  and no component. A converge compares the whole rendered unit and restarts
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

Today: `dotnet run --project ../../build/src/Wolfe.Lab.Build -- <job>`
from the component's directory, on a node with the .NET SDK (the Brewfile's
`dotnet-sdk` cask; the runner's PATH includes it). Each run compiles
from the checkout, which is seconds on the mini and needs no install.
Packing it as a tool and installing that on every node through chezmoi
is the next step; then the short-cycle workflows need no checkout at all.

CI builds, format-checks and tests this solution in the .NET SDK image
on the containerised runner (`build.yaml`), so a change that would fail on
the mini fails on the pull request.
