# Renovate — knowing what's stale

[Renovate](https://docs.renovatebot.com) reads every pin in the repo and
opens a pull request for each one that is behind. It merges nothing: a
pull request is a proposal, checked like any other, and a bump still goes
through the pin-first-via-a-normal-PR ritual the slice READMEs ask for —
Renovate only saves remembering to look.

## How it runs

`.forgejo/workflows/renovate.yaml`, every morning at 05:00 UTC and on
demand, on the containerised runner in Renovate's own image. It clones the
repo itself, as its own Forgejo user, `renovate`; its pull requests
therefore trigger every check exactly as a person's would. On a pull
request the same workflow validates `config.json5` (`renovate / check`, a
required status).

**One file, `config.json5`**, holds everything: where Renovate runs and
as whom, and what it watches in this repository. The repository has no
`renovate.json` and is not onboarded (`requireConfig: "ignored"`).

**Its identity is tofu's, in two roots.** `forgejo/tofu/renovate.tf`
creates the user and gives it write access to Wolfe.Lab alone — the
platform's job, with the owner's token. `renovate/tofu` then logs in *as*
the user to mint its token, because Forgejo creates a token only for a
request authenticated by password, and writes that token and a read-only
GitHub token (for lookups on github.com) as Actions secrets — the lab's
only ones: a containerised job never reaches the vault, and Renovate needs
nothing else from the node. They are two roots because the provider
connects as soon as it is configured: a root that logs in as the user
cannot also be the one that creates it, and `forgejo/tofu` must plan on a
Forgejo that has never seen it. What lives outside Forgejo is the bot's
password and the GitHub token, in the vault.

## What it watches

| Pins | Where | Notes |
|---|---|---|
| Container images | every `compose.yaml`, Dockerfile `FROM` | linuxserver and the mail bridge have their tag shapes spelled out; Immich's server and ML move together |
| The lab CLI | `ci/image/Dockerfile`, chezmoi's `install-lab` script | one pull request moves both; its body says to smoke-test the published package first (`build/README.md`, "Shipping") |
| NuGet packages and the .NET SDK | `build/`, `mail/watcher/` | grouped by family; a new .NET major waits on the dashboard until asked for |
| Actions | `.forgejo/workflows/*.yaml` | looked up on github.com; Forgejo's runner fetches the same actions from its own mirror |
| Tofu providers | every `tofu/` root, with the lock file | |
| Renovate itself | its workflow's `container:` image | weekly, not on every release |

The **dependency dashboard** is an issue on Wolfe.Lab that lists
everything pending, everything held back, and anything Renovate could not
look up.

## What it does not watch

Deliberately:

- **Immich's database and cache** (`immich-app/postgres`, `valkey`),
  pinned by digest in Immich's own compose file. They move when Immich's
  release notes say so.
- **`lab/mail-watcher`**, built on the node, never pulled.

Because Renovate cannot move them safely:

- **The Pi's chezmoi externals** — forgejo-runner, `op`, beszel-agent,
  node, restic — are pinned by version *and* checksum, and Renovate would
  bump one without the other. The Beszel hub's pull request carries a
  note to move the Pi's agent with it (`beszel/RUNBOOK.md`, "Two pins").
- **Build arguments with a twin elsewhere**: `NODE_VERSION` and
  `CHEZMOI_VERSION` in `ci/image/Dockerfile` track the nodes, and
  immich-go's `VERSION` carries its checksum.
- **The Brewfile** is unpinned, and **ollama's models** have no registry
  Renovate reads.

## Merging its pull requests

A Renovate pull request is merged like any other, and gets the same
`CHANGELOG.md` entry any change does — Renovate does not write one. A
bump to `build/Directory.Packages.props` changes what the CLI ships, so
`build / check` fails it until the pull request has a new version heading
(`build/README.md`, "Shipping").
