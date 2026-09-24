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
  on a Mac, systemd's user manager on Linux, chosen by the operating
  system at registration. The declaration is platform-neutral, so the same
  component deploys to either. On Linux the node needs lingering on
  (`loginctl enable-linger`) for its units to run with nobody logged in.
  A converge compares the whole rendered unit and restarts
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
What the sink cannot cover is a run that never reaches the CLI: a `lab`
that is missing or cannot start (the pin not installed, the runtime not
found) exits red without paging — and so does the CLI's own deploy if the
merged source fails to compile, which CI on the pull request is there to
catch first.

## Shipping

The CLI is itself a component: `build/ritten.json` is a `dotnet-tool`,
the package `Wolfe.Lab.Build` whose command is `lab`. **Its version is
the lab's** — `Directory.Build.props` reads the newest `## [x.y.z]`
heading of the repository's `CHANGELOG.md` — so there is one number and
one changelog, and a pin names the lab release whose entry says what that
CLI contains. The numbers have gaps: a lab release that does not touch the
CLI publishes nothing.

Its pull request is checked here — restore, format, build, test — and
one more thing: **a change to what ships must come with a new changelog
heading.** That is Ritten's continuous release cadence (`Ritten.NuGet`):
`ReadShippedChanges` diffs the project and the `Directory.*.props`
against the base, and `CheckVersion` fails a change whose version the
feed already has; tests and docs do not ship and do not count. The
merge's `deploy` restores, builds, tests and packs, and publishes that
version to Forgejo's NuGet feed
(`https://code.twolfe.dev/api/packages/tom-wolfe/nuget/index.json`),
stopping at the releasable gate when the feed already has it, so a
version is never overwritten and a pin always means what it meant. A
change under `build/` that ships nothing, after the lab has moved on, is
published under the new number: a harmless no-op release. The one step
of the lab's own is `AuthenticateFeed`, which reads the feed key from the
vault where Ritten's would read the environment.

Every machine on the tailnet has the feed as a NuGet source
(`chezmoi/home/dot_nuget/NuGet/NuGet.Config.tmpl`), mapped to
`Wolfe.Lab.*` alone so a laptop off the tailnet still restores everything
else from nuget.org: `dotnet tool install -g Wolfe.Lab.Build` works
anywhere, and `--version` matches what the runners are pinned to.

Publishing moves nothing. What runs on a runner is the version its pin
names, which is the point: **runners execute reviewed, merged code, never
a pull request's CLI.** A pull request that changes the CLI is built and
tested here and runs nowhere else. The cost is ordering — a slice change
that needs a new CLI behaviour lands after the CLI change has shipped and
its pin has moved, as two pull requests.

## How workflows run it

The pinned tool, `lab`, is on every runner. chezmoi installs it on each
node with a host runner (`.chezmoiscripts/install-lab.sh`, on the Mac
mini, the Pi and the Studio), and the CI image bakes the same version in
as its last layer (`LAB_VERSION` in `ci/image/Dockerfile`). The two pins
move together, in one pull request; the chezmoi and `ci image` workflows
roll them out on the merge.

Every workflow calls `lab <job>` from the component's directory, with
one exception: the CLI's own `deploy` compiles the checkout (`dotnet run`),
because its input is the CLI's source and, on a cold start, it is what
puts the first package on the feed. `setup.sh` compiles for the same
reason. On a cold start, then: Forgejo up (setup.sh), the `build` deploy
publishes, and the next chezmoi apply installs `lab` — the install script
runs on every apply and warns, rather than fails, while the feed is
unreachable.

The CI image (`ci/image/`) is an `image` component with the same two
jobs as everything else: `check` on the pull request (each context has a
Dockerfile, each tag names its registry — the containerised runner has no
Docker socket, so the build itself cannot be checked), and `deploy` on the
merge, which builds it on the Pi and pushes it to Forgejo's registry as
`code.twolfe.dev/tom-wolfe/ci`, so a containerised runner on a node that
did not build it can pull it.
