# chezmoi — the machine plane

`home/` is the chezmoi source: everything *declarative* about a machine —
dotfiles, the Brewfile, the service and runner files the nodes run under,
and, as `create_` files, the few secrets a node's own daemons read at
start (the runner registrations, the Beszel agent's token, the Pi's restic
key). Nothing a container needs is here: those secrets reach compose
through the deploy (`README.md` "How deployment works"). `.chezmoiroot`
points chezmoi at it.
Laptops apply it by hand; the nodes apply it through
`.forgejo/workflows/chezmoi.yaml` on the push that touches `chezmoi/`.

## Profiles

One prompted value, `profile`, says which machine this is: `macbook`,
`macstudio`, `work-macbook`, `macmini-node` or `pi-node`
(`home/.chezmoi.toml.tmpl`). There are no derived facts — no "is a
server", no OS test. A machine is its profile, and every file says what
each profile gets:

- **Root content is what every machine gets.** Anything else lives in a
  block that opens with one test, `{{ if eq .profile "…" }}`, one block
  per profile. Two profiles wanting the same lines repeat them: that is
  the point. It makes "truly common" a claim the file has to earn, and
  everything else an explicit opt-in per machine — the work laptop not
  listing Tailscale is a decision you can see, not a section it fell
  out of.
- **A template that renders nothing produces no file.** That is how a file
  is absent from a profile: the block isn't there. chezmoi does still
  create parent *directories*, so `.chezmoiignore` keeps one job —
  whole directories a profile does not have, one block per profile.
  Files never appear in it.
- **Scripts are one thing, gated whole.** A script opens with a single
  membership test (`has .profile (list …)`) rather than a copy per
  profile; a profile-specific step inside it is its own block.
- **`.chezmoidata.toml` is the inventory:** the facts a template needs by
  name (each node's runner name, which is also its vault-item suffix),
  looked up as `(index .profiles .profile).node`. Adding a machine is a
  new profile in the prompt's list, an entry there if it is a node, and
  a block in each file it should get.

**Seeing it.** `lab check` from `chezmoi/` renders the whole source for
every profile in `ritten.json`, on any machine, vault stubbed, lists what
each machine would get (`--verbose`) and shellchecks the rendered scripts.
The chezmoi workflow runs it on each pull request, so a template that only
breaks on the Pi fails before the merge.

## Packages

`home/dot_Brewfile.tmpl` renders to `~/.Brewfile` and stops there. It
*declares* the machine's package set. Acting on it:

| Job | When | Does |
| --- | --- | --- |
| `.chezmoiscripts/00-install-packages.sh` | every `chezmoi apply`, every machine | `brew bundle install --no-upgrade` — installs what's missing, nothing more |
| `.forgejo/workflows/chezmoi.yaml` | the push that touches `chezmoi/` | runs `chezmoi update` on each server, which is the apply above |
| you, at the desk | when you choose | `brew bundle install --file ~/.Brewfile --upgrade` on the mini, then `chezmoi apply` — the only thing that moves versions there |

### Why installs and upgrades are separate

The Brewfile used to live *inside* a `run_onchange_` script, inlined by
`{{ template "Brewfile" . }}`. chezmoi keys those scripts on a hash of the
rendered script, so `brew bundle` ran when the Brewfile changed — and since
**`brew bundle` upgrades outdated formulae by default**, every version bump
on the mini was triggered by whatever unrelated edit happened to touch that
file. Adding one VS Code extension swept the entire toolchain along with it;
two quiet months would have frozen it silently. The mini was current only by
accident.

`--no-upgrade` is the whole fix: installing what is declared and moving
versions forward are different jobs with different reasons to happen.

### Why upgrades are manual

There is nothing to pin to. `brew "foo"` *means* "the current formula";
versioned formulae like `node@22` exist only where upstream publishes them
and are separate packages, not pins; casks and `mas` have no version
selection at all; and Homebrew deletes old bottles. Unlike `caddy:2.10` —
an immutable artifact that will still resolve next year — there is no
artifact to name. Renovate can't help either: its `homebrew` manager matches
`^Formula/**.rb` (formula files inside a tap), not Brewfiles.

So the repo's usual discipline — bump a pin in a PR, let the deploy flow
act on it — has nothing to bite on, and the alternative once tried here, a
nightly unattended upgrade job (0.11.0 → 0.18.1), was the
wrong trade for a lone server: it moved every package at once, unreviewed,
and it failed every night regardless, because `.pkg`-based casks
(`dotnet-sdk`) install through sudo and a forced-command SSH session has
no password to give. Versions on the mini now move only when a person
moves them:

```sh
brew bundle install --file ~/.Brewfile --upgrade
```

Bundle rather than plain `brew upgrade`, because only bundle honours
`restart_service: :changed` — `brew upgrade` swaps a binary and leaves the
old process running, which for the Beszel agent means a monitor silently
running stale code. `brew pin <formula>` holds anything that must not move;
pinned packages are skipped.

### Notes

- The install script sets `HOMEBREW_NO_AUTO_UPDATE=1`: the lab is meant to
  keep working with the internet down, and with no scheduled upgrade nothing
  on the mini refreshes Homebrew's metadata except the hand-run upgrade
  above, which does it first.
- Editing the Brewfile: `chezmoi apply` on a laptop installs immediately;
  on a server the merge does, through the `chezmoi` workflow.

## Pushes

`.forgejo/workflows/chezmoi.yaml` runs `chezmoi update` on each node
when a push touches `chezmoi/`. Nothing here needs a schedule: config
changes arrive as pushes, and `create_` files are written on the apply
that first needs them.
