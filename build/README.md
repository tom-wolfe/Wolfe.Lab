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

Workflows so far: `obsidian` (`sync --vault <name>`).

## Clients

Ritten's own where they exist — `IGit` for every git operation, its
command runner for every process. The lab adds what Ritten doesn't have:

- **Secrets** — `ISecrets` reads one `op://` reference through the
  1Password CLI, authenticated the way `scripts/secrets.sh` is. A
  setting names a secret; a step reads it at the moment of use, so a
  job that has nothing to push never touches the vault.
- **Obsidian** — `IObsidian` runs the headless client through the
  `nvm-run` wrapper chezmoi installs.

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
