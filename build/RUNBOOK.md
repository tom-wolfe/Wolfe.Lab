# build runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Running a job by hand

From the component's directory, with `--dry-run` unless the side effects
are wanted; without it the gate asks before the push. `lab` is the pinned
CLI chezmoi installs on the nodes and the Studio — the version the runners
run:

```sh
cd obsidian/vaults
lab sync --vault main --dry-run
lab sync --vault main
lab --help
```

To run an unreleased CLI — a change being written — compile the checkout
instead, from the same directory:

```sh
dotnet run --project ../../build/src/Wolfe.Lab.Build -- sync --vault main --dry-run
```

## Building against an unreleased Ritten

`Directory.Packages.props` pins the Ritten version the solution needs.
When that version is not on NuGet yet — the lab tends to need what was
just added — pack it from the Ritten checkout into a local feed and
restore from there, with the package cache redirected so the local
build never shadows the published one:

```sh
feed=/tmp/ritten-feed; cache=/tmp/ritten-cache
for p in Ritten.Core Ritten.CommandLine Ritten.Git Ritten.Docker Ritten.DotNet Ritten.Forgejo Ritten.NuGet Ritten.OnePassword Ritten.OpenTofu; do
  dotnet pack ~/Development/Ritten/Ritten/src/$p/$p.csproj -c Release -p:Version=<version> -o "$feed"
done
cat > /tmp/nuget.unreleased.config <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF
export NUGET_PACKAGES="$cache"
dotnet restore build/Wolfe.Lab.Build.slnx --configfile /tmp/nuget.unreleased.config
dotnet build build/Wolfe.Lab.Build.slnx --no-restore
dotnet test --solution build/Wolfe.Lab.Build.slnx --no-build
```

A config file of its own rather than `--source`: the user-level
`NuGet.Config` every lab machine has maps `Ritten.*` to nuget.org alone
(package source mapping, for the lab's feed), and a mapped package is
never looked for anywhere else — the local feed would be ignored. The
throwaway config clears the sources, and with them the mapping.

Nothing merges until the version is published: the mini restores from
NuGet alone.

## Releasing a CLI change

Add the lab's next `## [x.y.z]` heading to `CHANGELOG.md` in the same
pull request, describing the change — the CLI's version is read from it,
and the check fails a change to what ships without one. The merge
publishes it.
Then move the pin to it in a second pull request — the version in
`.config/dotnet-tools.json`, which chezmoi and the CI image both read —
which is what actually puts it on the runners.

## The package token

One Forgejo access token publishes both the CLI package and the CI image:
the `PACKAGES_TOKEN` Actions secret on Wolfe.Lab, scope **package: read
and write** and nothing else, belonging to `tom-wolfe` (packages under a
user are writable only by that user). Both publishes run in the
containerised pool, which never reaches the vault, so `forgejo/tofu`
(`packages.tf`) writes it from the vault into the secret. Made by hand,
not minted: minting a user's token means logging in as that user, and
no root should hold the owner's password. Reading needs no token:
packages under a public owner are public. Forgejo's automatic Actions
token cannot stand in for it — it has no package access at all.

Making or rotating it: in Forgejo, as `tom-wolfe`, Settings →
Applications → Generate New Token, scope **package: read and write** and
nothing else; replace `forgejo-packages` → `credential` in the Wolfe.Lab
vault; re-run the `forgejo tofu` workflow, which writes the secret; then
revoke the old token.
