# obsidian

The vaults — `Main` and `Dungeons & Dragons` — live in Obsidian Sync.
The mini keeps a headless copy of each under `~/Obsidian/<name>` and
turns it into git history on Forgejo. Each vault is a component of its
own, `main/` and `dnd/`, whose `component.yaml` declares where it is
checked out, where it pushes, what git leaves out and how the push
authenticates; `lab sync` from its directory (the CLI in `platform/lab/`)
runs one pass: sync, commit what changed, push what Forgejo lacks. One workflow
per vault, because each has its own rhythm: `main` every ten minutes;
`dnd` daily, which makes a game night one commit.

Git buys three things Obsidian Sync does not: history without a
version window, a copy that is not Obsidian's, and a backup for free —
the repositories sit in Forgejo's data directory, which restic
snapshots nightly. The checkout on the mini is a working copy and is
not backed up itself; Forgejo is the record. Anything else that wants
the corpus (a local model on the Studio, say) clones the repository
rather than reading a mount.

## The mini is a mirror

Its copies run in Obsidian Sync's `mirror-remote` mode: download only,
local changes reverted. The checkout is therefore exactly the vault as
the other devices see it, and nothing the mini holds — including the
settings stub the headless client writes at setup — can flow back up.
Config sync is on for the mini so that `.obsidian/` in git is the
vault's real settings; it carries only the categories the other
devices publish, so their "Vault configuration" toggles decide what
arrives.

## What's committed

Everything Obsidian Sync delivers: notes, attachments and `.obsidian/`.
Each vault's `exclude` list is written into its checkout's
`.git/info/exclude` on every pass, so nothing lab-owned lives inside
the vault where it would sync to every device. It holds `.DS_Store`,
Obsidian's `.trash/`, and the client's `.sync.lock`.

## Pushing

`push.token` names the secret: `forgejo-obsidian-token`, a Forgejo
access token scoped to `write:repository` and nothing else. The CLI
reads it only when there is something to push, and hands it to git
through a one-shot credential helper, so it never lands in the remote
URL, the checkout or the process list.

The repositories are declared in `platform/forgejo/tofu/vaults.tf` —
private, git only, `prevent_destroy`. Adding a vault is one entry
there, a directory with its `component.yaml`, one workflow, and the
bootstrap in `RUNBOOK.md`.
