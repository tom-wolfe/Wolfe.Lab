# build runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Running a job by hand

From the slice's directory, with `--dry-run` unless the side effects
are wanted:

```sh
cd obsidian
dotnet run --project ../build/src/Wolfe.Lab.Build -- sync --vault main --dry-run
dotnet run --project ../build/src/Wolfe.Lab.Build -- sync --vault main   # asks before the push
dotnet run --project ../build/src/Wolfe.Lab.Build -- --help
```

## Building against an unreleased Ritten

`Directory.Packages.props` pins the Ritten version the solution needs.
When that version is not on NuGet yet — the lab tends to need what was
just added — pack it from the Ritten checkout into a local feed and
restore from there, with the package cache redirected so the local
build never shadows the published one:

```sh
feed=/tmp/ritten-feed; cache=/tmp/ritten-cache
for p in Ritten.Core Ritten.CommandLine Ritten.Git; do
  dotnet pack ~/Development/Ritten/Ritten/src/$p/$p.csproj -c Release -p:Version=<version> -o "$feed"
done
export NUGET_PACKAGES="$cache"
dotnet restore build/Wolfe.Lab.Build.slnx --source "$feed" --source https://api.nuget.org/v3/index.json
dotnet build build/Wolfe.Lab.Build.slnx --no-restore
dotnet test --solution build/Wolfe.Lab.Build.slnx --no-build
```

Nothing merges until the version is published: the mini restores from
NuGet alone.
