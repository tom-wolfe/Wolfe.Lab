# lab

The lab's jobs as a CLI, `lab`, built on [Ritten](https://github.com/ritten-org/Ritten):
`Wolfe.Lab` in `tool/src/`, its tests in `tool/tests/` — `tool/` is the
component that builds and publishes it, and #6's agent will be `agent/`. A workflow file is a
trigger and one command; what the command does lives here, in C#, where
it can be typed, rehearsed and tested.

## How a component opts in

A directory the CLI serves carries a `ritten.json` naming its workflow
— `"workflow": "docker"` — plus that workflow's options. That directory
is a *component*: a service is the folder that groups its
components (`media/sonarr/compose/`, `media/sonarr/backup/`, `platform/forgejo/tofu/`,
`platform/forgejo/runners/`), and never carries a declaration of its own. Run
from a component's directory, `lab` offers exactly that workflow's jobs
as commands, each option of which is a job argument the job declared. A
workflow is a class here: its jobs, each job's ordered steps, and the
options shape its `ritten.json` must satisfy, judged before anything
runs. Steps hand each other typed values (a `Release`, say) rather than
sharing state, and reach outside the working directory only through a
client that has a dry-run twin, so `--dry-run` rehearses any job
without a side effect.

A workflow is a *shape* of component, not a service. The regular shapes
carry most of the lab: `docker` (a compose stack — check on the pull
request, deploy on the merge), `tofu` (a root module — plan on the pull
request, apply on the merge), `backup` (what a snapshot holds, what has
to be quiet while it is taken, and what a restore must bring back),
`image`, `dotnet-service` and `agents` (host processes — the node services
that used to live in chezmoi — placed on the nodes they run on, and
deployed to each by its own runner, which runs `lab deploy` on the node it converges). Two services with the same shape share a
workflow and differ only in what they declare. What only one service does
is a component of its own with a workflow of its own — `network/caddy/certs`,
`network/caddy/routes`, `platform/forgejo/runners`, `platform/garage/layout`, `personal/immich/import`,
`monitoring/gatus/health` — named for the service and the thing, so a `ritten.json`
reads as what it is. Nothing is shared *across* workflows: a step two
workflows need belongs to a domain module under `Clients/`, and each
workflow lists it for itself.

A job's name has to read from inside the component it runs in, because
that is all the context there is: `renew` in `network/caddy/certs/`, `register`
in `platform/forgejo/runners/`, `init` in `platform/garage/layout/`, `verify` in
`platform/restic/repositories/`. One intent gets one verb, too: making a node
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

Every container says where in the lab it lives. The component's path is
`<area>/<service>/<component>`, which is the label schema (ROADMAP.md
#11) already, so the deploy writes a `compose.override.yaml` into the
release — compose merges it by itself — putting `lab.area`,
`lab.service` and `lab.component` on every service as Docker labels,
which the collector reads a container's logs under, and as
`OTEL_RESOURCE_ATTRIBUTES`, which an application sending its own
telemetry reads. No compose file in the repository carries them, so none
can disagree with where it sits; the file is the deploy's, so the
release's mirror leaves it in place. Each service of the stack is a
component of its own, labelled with its own name: the deploy finds the
components its directory declares in the catalog, so a stack whose
services are not all declared — with their service — fails the deploy
rather than going unlabelled. The same file carries what each component
asks of its service — its `logs` and `metrics` (Declarations, below).

`network/caddy/routes` is the one component that reads other components, and
deliberately: it gathers every `caddy.caddyfile` in the checkout into a
release of its own that the door's Caddyfile imports, so adding a
service never edits the front door, and a route added in the same push
as its stack does not depend on which workflow the runner reached first.

A backup component snapshots a release's state. Its `ritten.json` names
the release (the snapshot's tag and, when a container is named in
`stop`, the installed stack that is stopped for the duration), the
paths, the excludes, and the `verify` paths a restore must bring back.
The repositories themselves are the restic service's to define: every
backup component reads `platform/restic/restic.env` (or `sftp.env` on a Linux
node) by walking up to the checkout — the one file a component reads
outside its own directory, because a copy in every component would be a
copy that drifts.

A tofu root runs under the same state backend, declared once in
`platform/garage/tofu-state.env` — Garage's, since it serves the state —
and found by walking up from the root, plus its
own `secrets.env`; both name secrets by reference, read through Ritten's
provider as each command starts.

### Artifacts

A deploy publishes files to the node as **artifacts**: a directory of the
component, mirrored to an output directory, declared in `ritten.json`:

```json
"artifacts": [{ "source": "config", "output": "${LAB_ROOT}/alloy" }]
```

A compose component's release is one already — the component itself,
published to `${LAB_ROOT}/<release>` — and it may declare more; an agents
component declares the ones its agents read, and names them in their
arguments the same way (`"run", "${LAB_ROOT}/alloy/config.alloy"`). An
output is a mirror, so what its source lacks is deleted: it must lie
strictly inside `${LAB_ROOT}`, and no two outputs may nest. The run's
report has an **Artifacts** section: each artifact, where it went, the
files that changed (`+` added, `~` changed, `-` deleted) folded beneath it,
and any restart that followed — a rehearsal reports the same as what it
would do.

A published file is written only when its content changed, so the newest
write under the outputs says when the artifacts last did, and that moves
the runtime: an agent's unit carries it (as it carries its binary's), so a
changed config reloads the agent; a compose stack is restarted, and the
time it was restarted for is kept in `${LAB_ROOT}/.applied/<release>`,
written only once the restart has happened. `up` alone would recreate a
container whose definition changed, never one whose bind-mounted file did.
Both read the state of the node rather than of the job, so a restart that
failed is still owed on the next deploy.

### Packages and tools

The lab installs what it runs, from GitHub releases, at versions the
repository pins — rather than whatever a node's Homebrew last upgraded
to. One installer, two kinds of declaration:

- **An agent's `package`**, in its component's declaration: the release
  its program comes from, its asset named for each node by `{platform}`.
  The deploy installs it before resolving the agent, and `{package}` names
  its directory:

  ```yaml
  package:
    github: grafana/alloy
    version: 1.20.1
    asset: "alloy-{platform}.zip"
    checksums: SHA256SUMS
  program: "{package}/alloy-{platform}"
  ```

- **A tool**, in `.config/lab-tools.json`: a program jobs run by name —
  `tofu`, `restic` — with an asset per platform (`darwin-arm64`,
  `linux-arm64`), since any node may run it. A job that runs one lists
  `EnsureTools` first; it installs the pinned version and puts it first on
  the path of `lab` itself, so every command it starts runs that one —
  from `bin` inside the package when the archive nests it. A tool the
  manifest does not pin is the node's own.

A package is downloaded, checked against the checksum file the release
publishes beside it (`checksums`) — or, for a project that publishes
none, against the SHA-256 GitHub records for the asset — and unpacked — zip, tar.gz or a single
compressed or bare file — into `${LAB_ROOT}/packages/<name>/<version>`,
beside the directory and renamed into place, with a marker written last,
so a version that exists was installed whole. Versions sit side by side:
a bump installs a new directory, which changes the agent's unit and
restarts it, and rolling back is reverting the bump. `{version}` in any
name is the version; the tag is `v{version}` unless `tag` says otherwise.
Whatever the lab runs out of a package is made executable, since not
every release ships it so. A rehearsal installs nothing, but checks the
release has both files.

Only the version is pinned. The checksum is the release's own, read over
the same TLS as the asset, which proves the download is whole rather than
who made it — and is what keeps one value per package for Renovate to
move (`github`, then `version`, in that order: its rule reads them as a
pair).

### Two roots

Every node keeps the lab in two places, declared in `platform/nodes.yaml`
as its `root`, where components and their artifacts are installed
(`~/.local/share/Wolfe.Lab`), and its `data`, where services keep their
state (`~/Docker`). That declaration is their one source. chezmoi
exports a replica of it for the host runners' jobs and for every shell
on a node, as `LAB_ROOT` and `LAB_DATA`, looked up by the node's name
(`.chezmoitemplates/lab-node.tmpl`), and exports that name as `LAB_NODE`
(`mini`, `studio`, `pi`), which a deploy reads rather than being told. A
laptop is no node, and has none of the three. An agent deploy still
refuses a node whose declaration and environment disagree, which is
only possible between a change to `nodes.yaml` and chezmoi applying it.
A `ritten.json` artifact output names the roots as `${LAB_ROOT}` and
`${LAB_DATA}`, and nothing else in one is expanded; a declaration names
them as `{lab.root}` and `{lab.data}`.

### Declarations

The lab is moving from `ritten.json` to describing itself in YAML files
of its own (ROADMAP.md #14): a `kind: service` document in each service's
directory — its catalog entry — and a document per component, a logical
part of the service: `kind:` saying what it is used for and `workflow:`
the workflow that operates it, by the CLI's own name (`kind: database`,
`workflow: docker`), each a closed set and neither implying the other.
The workflow is the document's discriminator: one that reads more than
every component declares says so in a shape of its own. By
convention they are `service.yaml` and `component.yaml`, but a file's
name decides nothing, and one file may hold several documents split by
`---` — a compose stack's components, beside its `compose.yaml`. Every
component names itself: where a file sits says which service it belongs
to, never what anything in it is called. A file is the lab's when its
first line names the schema, relative to itself:

```yaml
# yaml-language-server: $schema=../../platform/lab/schema/lab.schema.json
kind: service
name: immich
description: The photo library, and the phone app's server.
```

That line is also what gives an editor the rules: completion, and a
description of every key. The schema is generated from the declaration
types in `Wolfe.Lab.Infrastructure/Declarations`, never written by hand:
every local build writes it, through the CLI's hidden `lab schema
<file>`, so the committed copy is the branch's. CI does not write it, and
a test there fails when the committed copy is stale. The CLI a node runs
is the pinned one, so between a change to the declarations' types and
the pin bump that ships it, an editor reads a schema the pinned CLI does
not yet judge by. Only files git tracks are read, three
directories deep at most.

Each component's check job reads the catalog (`CheckServiceCatalog`,
straight after the path filter), so a problem is reported on the pull
request that made it. There is one way a catalog is read,
`ServiceCatalogReader`, and it is valid or it is every problem, each
pointed at its file and line: one anywhere fails every check, as it
would fail every deploy — so `main` only ever holds a valid one. The
reader judges the YAML and its shape against the schema; every rule of
the catalog is the domain's, each held by what owns it. A service or
component is made by its type's `Create`, which refuses what cannot be
one — a file out of its place, a name not its directory's, a reference
to itself — and then added: `ServiceCatalog.Add` refuses a service of a
name or directory already taken, or depending on one the catalog has
not got; `Service.Add` refuses a component out of its service's
directory, of a sibling's name, or part of or depending on a sibling the
service has not got. References stay names, found through
`ServiceCatalog.FindService` and `Service.FindComponent`. A catalog is valid from
empty and stays so. The reader only chooses an order a valid catalog can
be built in — a service after the services it depends on, a component
after what it is part of or depends on. While a component has a
`ritten.json` as well, the directory's components must declare the
workflow it names — `workflow: docker` beside a `"workflow": "docker"` —
but for a part of one (`partOf`), which names its own.
The CLI's own tests hold the whole repository's declarations, a service's
entry included.

A component the `docker` or `dotnet-service` workflow operates names the
compose service it runs as, so the component's name is the catalog's and
the container keeps the unique name Docker gives it; and it may carry two
facets so far:

```yaml
name: server
kind: app
workflow: docker
service: immich-server       # the compose service it converges
logs: otlp                   # sends its own; its output is left alone
metrics: { port: 8081 }      # the container's port; path /metrics unless given
```

`metrics` is one endpoint or a list of them, and an endpoint may say
where on the host it is `published` when its container port is taken
there already (`{ port: 3000, published: 13000 }`).

The check, and the deploy again, hold the directory's `compose`
components to its stack exactly: every service is one component's, and
every component names a service the stack has. The deploy writes the
`logs` facet into the `compose.override.yaml` as the `lab.logs` label the
collector reads, and the component's metrics endpoints into
`${LAB_ROOT}/.metrics/<component>.json`, the targets file the collector
scrapes (its own file, so a component that stops declaring them is
dropped). The collector, a host process, reaches a container only through
a published port: one the file publishes already on `127.0.0.1` is
scraped where it is, and any other is published on `127.0.0.1` alone.
Refused too: metrics on
a service in another's network (it has no ports of its own), and logs
declared both on the component and by a `lab.logs` label in the compose
file, which is still read until each moves; a compose file may never set
`lab.metrics.*`.

The machines are declared too, in `platform/nodes.yaml`, a `kind: node`
document each: its name, its `role` (`server` or `hybrid`), its
`platform` as release assets name it, its tailnet `address`, its two
roots (`root` and `data`, written whole: a `~` or a variable would mean
whatever it means on the machine reading the file), its Docker socket and
its drives. Which node a deploy is on is the node's to say —
`LAB_NODE`, set by chezmoi (see "Two roots") — never an argument to it. They are added to the catalog first, so a component can
be placed on them.

A component the `agents` workflow operates is one host process, declared
once for every node it runs on:

```yaml
name: forwarder
kind: collector
workflow: agents
runsOn: all                  # or 'every server', or [mini, pi]
agent: alloy                 # its unit is dev.twolfe.alloy, its logs' service_name alloy
package: { github: grafana/alloy, version: 1.20.1, asset: "alloy-{platform}.zip", checksums: SHA256SUMS }
program: "{package}/alloy-{platform}"
arguments: [run, "{lab.root}/alloy/forwarder.alloy", "--storage.path={state}"]
environment:
  LAB_GATEWAY: "{node.mini.address}"
  DOCKER_HOST: "{node.docker}"
```

What differs between its nodes is the nodes' to say, through one
placeholder syntax, `{dotted.lower}`: `{lab.root}` and `{lab.data}`, the
two roots; `{state}`, the service's state directory under `{lab.data}`;
`{package}`, where its package is installed; `{platform}`, or
`{platform.os}` and `{platform.arch}` apart; `{node.name}`, `{node.role}`,
`{node.address}`, `{node.docker}` and `{node.drives}` (comma-separated);
and `{node.<name>.address}` for another node. A package's asset and
checksums take `{version}` and the platform alone. A placeholder the lab
does not have is refused where it is written, and one a node has no
value for — `{node.docker}` on a node without Docker — by the check, for
that node. `LAB_HOST`, `LAB_ROLE` and `LAB_ROOT` are the node's, set for
every agent and declared by none. Where its output goes is the node's
too: both streams to `{lab.root}/logs/<area>-<service>-<component>.log`,
on every node alike, declared to the collector by the deploy, which
retires any targets its agents left under an earlier name. A deploy on a
node the component is not placed on has nothing to do.

An agent's log is kept to a size by `lab rotate`, a job of the agents and
ollama workflows: logrotate, as the lab's user, with the component's own
configuration and state under `{lab.root}/.logrotate`, rotating past 10 MB
and keeping five compressed generations. It copies a log and truncates it
in place rather than moving it, because launchd and systemd hold the file
open for as long as the agent runs. chezmoi installs logrotate on the
Macs, and the Pi's OS ships it; nothing else runs it. A rehearsal is
logrotate's own debug run, from the run's scratch.

`runsOn`, `agent` and `program` are required of every agents component;
its `ritten.json` declares only its `artifacts`. An `ollama` component
declares its agent the same way, required of it too, placed on its one
node (`runsOn: [mini]`), while its models and roles stay in its
`ritten.json`. Its deploy holds `models.store` to the agent's
`OLLAMA_MODELS`.

## Layout

`tool/src/` holds four projects, in layers (ROADMAP.md #14), and the
references between them are the only direction a dependency can point:

- `Wolfe.Lab.Domain` — the lab's model: the value types the rest is typed
  in (`HostPath`, `RepositoryPath`, `Port`), the catalog — each service
  the aggregate of its components, which read their area from it, and
  their facets — the telemetry attributes, a backup's retention:
  by concept, one folder each (`Paths/`, `Network/`, `Catalog/`,
  `Telemetry/`, `Backups/`). Docker is one workflow among the catalog's: a
  `docker` component names its compose service, and the infrastructure
  binds the two. No reference
  beyond Ritten's `Result` and `Error` and its file system abstraction,
  `IDirectory` and `IFile`: the domain says where a file is and what is in
  it, and a test hands it a fake.
- `Wolfe.Lab.Infrastructure` — the outside world, one folder per module:
  a client, its dry-run twin and the `AddX()` that registers the pair.
  `Releases` (the installer and where a component is released to),
  `Volumes` (the mounted-drive guard), `Restic`, `Secrets`, `Heartbeat`,
  `Alerts`, `Agents`, `Caddy`, and the rest. The domain opens its own
  files through `IDirectory` and `IFile` (`HostPath.Directory`); what
  reads a disk, a process or the network directly is here.
- `Wolfe.Lab.Application` — what the lab does:
  - `Workflows/<name>/` — one per `"workflow"` a `ritten.json` can name,
    the folder named for the workflow (`CaddyRoutes/` for
    `caddy-routes`): the workflow class, its options record under
    `Models/`, and the jobs and steps only it lists under `Jobs/` and
    `Steps/`. Where several names are one family (`docker`, `image`,
    `dotnet-service`) they share a folder.
  - `<Module>/` — the steps more than one workflow lists, named for the
    infrastructure module they consume (`Agents/`, `Releases/`), or for
    what they do when they consume none (`Gates/`).

  `LabJob`, the base every job shares, sits at its root.
- `Wolfe.Lab` — the CLI, a thin host: `Program`, the workflows it
  registers (`LabApplication`), its runtime and `appsettings.json`. It is
  the package the runners install, and carries the other three inside it.

Nothing in Infrastructure knows a workflow exists, the domain knows
neither, and no workflow knows another. The agent (#6) will be a fifth
project beside the CLI, over the domain and the infrastructure.

A bound shape is an *options* type, whichever file it comes from — a
workflow's `ritten.json` (`ResticOptions`) or a client's section of
`appsettings.json` (`HealthchecksOptions`) — as it is in
`Microsoft.Extensions.Options`; `WorkflowSettings`, `SettingsValidator`
and `ValidateSettings` are Ritten's names, not the lab's. Production code
has no null-forgiving operator (`!`): a value validation has already
promised is taken with a pattern (`is { } path`), or through an accessor
that throws saying which setting is missing.

An error is a well-known one, never written where it is returned: each
concept has an `…Errors` class beside it (`ServiceErrors`,
`ComponentErrors`, `ComposeFacetErrors`), as ErrorOr has them — a
property for one that takes nothing (`ComponentErrors.NeedsAName`), a
method for one that does (`ComponentErrors.DependsOnItself(name)`) — so
a caller can ask which it got: `error == ComponentErrors.NeedsAName`.
Where an error is, is kept beside it rather than written into it: a
`CatalogError` holds the document, line and field around its `Problem`,
and a `FieldError` the field, so the problem stays the error it was.

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
  and writes nothing. An environment value that is a vault reference is
  resolved at deploy, and every unit is written for its owner alone
  (`600`), since that is where the secret lands. An agent can name the
  units it `supersedes` — Homebrew's `sh.brew.<name>`, a unit chezmoi used
  to write — and the converge stops and removes them before starting it,
  file and all, so the old copy does not come back at the next login.
  `program` and `log` are paths, and `~` in them is expanded; an
  environment value is handed to the process exactly as written, so one
  that starts with `~` is refused — nothing between the declaration and
  the process would expand it. After converging, the deploy says which
  log files the component's agents write: a target file per component in
  `${LAB_ROOT}/.logs/<area>-<service>-<component>.json`, which the
  node's collector discovers (monitoring/alloy), each `log` named for its
  agent and labelled with where the component lives — the placement a
  compose deploy labels its containers with. The file is rewritten
  whole, so an agent dropped from the component, or no longer given a
  `log`, stops being read. chezmoi declares the one host process that is
  not a lab agent the same way: the runner on a Mac, whose log its plist
  names. A target's labels are held to the attribute list like any other
  file (`CheckTelemetryNames`), chezmoi's included.

### Waiting and retrying

Resilience is a client's implementation detail, never a step's. A step
asks for what it needs — `garage.AwaitReady()`, `ollama.AwaitServing()`,
the supervisor converging an agent — and reports the answer; how long
that may take, how often to ask again and what to retry live inside the
client, which registers them with itself (`AddGarage()`, `AddOllama()`,
`AddAgents()`).

Inside a client, anything that waits or retries goes through
`Microsoft.Extensions.Resilience` (Polly), never a loop of its own. A wait
is a **polling** pipeline (`Clients/Resilience/Polling.cs`): the question
is asked again at an interval until it answers yes, and the wait gives
up at a limit, which a call may set for itself (an agent's exit
timeout). It runs on the injected `TimeProvider`, so a test moves a fake
clock rather than sleeping. An HTTP client gets the standard resilience
handler (`AddConfiguredResilience`).

None of the numbers are in code. **`appsettings.json`**, shipped beside
the CLI in the tool, holds how the CLI behaves and where it reaches out
to — each wait's `Limit` and `Interval` under its client's section
(`Garage:Answering`, `Ollama:Serving`, `Launchd:Unloading`), each HTTP
client's `HttpStandardResilienceOptions` (`Packages:Http`, `Gatus:Http`),
the services it calls (`Alerts:Endpoint`, `Heartbeat:Endpoint`,
`Packages:Releases` and `Packages:Api`) and the vault's service account
(`OnePassword:ServiceAccountTokenFile`) — where a component's
`ritten.json` holds what the component is, including a fact another
component defines and it needs, such as the container and Caddyfile path
of the caddy it reloads (`caddy`). What stays in code is the contract the
repository is written against — `secrets.env`, `lab-tools.json`, the
`.lab-volume` sentinel — where a setting would only be a way for a node
to disagree with the repository. Environment
variables prefixed `LAB_` override it on a node
(`LAB_Ollama__Serving__Limit=00:01:00`). A section that is missing, or a
limit of zero, fails as the client is built, and a test binds every
section of the shipped file.

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

The CLI is itself a component: `tool/ritten.json` is a `dotnet-tool`,
the package `Wolfe.Lab` whose command is `lab`. **Its version is
worked out, never typed: one more than the last release, when what ships
has changed since it.** The releases are the tags, `lab/v1.0.<n>`, and
what ships is the project, the `Directory.*.props` and the SDK the
repository pins in its root `global.json`; the deploy's first step,
`ComputeVersion`, diffs those against the last release's tag and writes
the version to `temp/version.props`, which `Directory.Build.props`
imports. A merge that changes none of them keeps the last release's
version, so the gate below finds it on the feed. The deploy runs the
checkout's own CLI, so the rule is in C# and tested, and the workflow
computes nothing. The number says one thing — the CLI's *n*th release —
and its tag names the commit; a move is one change like any other, so
nothing has to remember where the CLI used to live. Built anywhere else,
it is `1.0.0-local`, which no feed holds.

Its pull request is checked here — restore, format, build, test. The
merge's `deploy` restores, builds, tests and packs, tags the commit
`lab/v1.0.<n>`, and publishes that version to Forgejo's NuGet feed
(`https://code.twolfe.dev/api/packages/tom-wolfe/nuget/index.json`). A
merge that changes nothing that ships keeps the number, so the releasable
gate finds it on the feed and stops: no empty release, and no pin to
move. A version is never overwritten, so a pin always means what it
meant. The changelog is dated rather than versioned: what changed, on
which day, whether or not it shipped a CLI. The one step
of the lab's own is `AuthenticateFeed`, which reads the feed key from the
vault where Ritten's would read the environment.

Every machine on the tailnet has the feed as a NuGet source
(`platform/chezmoi/home/dot_nuget/NuGet/NuGet.Config.tmpl`), mapped to
`Wolfe.Lab` and `Wolfe.Lab.*` alone so a laptop off the tailnet still restores
everything else from nuget.org: `dotnet tool install -g Wolfe.Lab` works
anywhere, and `--version` matches what the runners are pinned to.

Publishing moves nothing. What runs on a runner is the version its pin
names, which is the point: **runners execute reviewed, merged code, never
a pull request's CLI.** A pull request that changes the CLI is built and
tested here and runs nowhere else. The cost is ordering — a service change
that needs a new CLI behaviour lands after the CLI change has shipped and
its pin has moved, as two pull requests.

## How workflows run it

The pinned tool, `lab`, is on every runner. The pin is one line: the
repository's tool manifest, `.config/dotnet-tools.json`. chezmoi installs
that version on each node with a host runner (`.chezmoiscripts/install-lab.sh`,
on the Mac mini, the Pi and the Studio), and the CI image bakes it in as
its last layer (`platform/ci/image/Dockerfile`, which reads the manifest with jq);
the chezmoi and `ci image` workflows both roll it out on the merge. On a
laptop, `dotnet tool restore` then `dotnet lab` runs the same version
from anywhere in the repository.

Every workflow calls `lab <job>` from the component's directory, with
two exceptions. The CLI's own `deploy` compiles the checkout (`dotnet run`),
because its input is the CLI's source and, on a cold start, it is what
puts the first package on the feed. The CI image's `deploy` runs kaniko
directly (below), because the image `lab` ships in is the one it builds. `setup.sh` compiles for the same
reason. On a cold start, then: Forgejo up (setup.sh), the `lab tool` deploy
publishes, and the next chezmoi apply installs `lab` — the install script
runs on every apply and warns, rather than fails, while the feed is
unreachable.

The CI image (`platform/ci/image/`) is an `image` component, and the one with a
`check` and no `deploy`: the check runs on the pull request (each image's
Dockerfile exists, each tag names its registry), and the merge's build
and push are kaniko's, in `ci-image.yaml`, to Forgejo's registry as
`code.twolfe.dev/tom-wolfe/ci`, where every containerised runner pulls it.

Both publishes run in the containerised pool and touch no node. The CLI's
runs in the CI image, with the SDK the root `global.json` names — the one SDK pin, which
chezmoi installs on the nodes and every .NET project here builds with. The CI image's
runs in kaniko's own image, not the one it builds — so a broken CI image
never blocks its fix — and kaniko builds a Dockerfile unprivileged with
no daemon, where the pool hands no job a socket. Both push with one
package-only token, the `PACKAGES_TOKEN` Actions secret (`RUNBOOK.md`,
"The package token").
