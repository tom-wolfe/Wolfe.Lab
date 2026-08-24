# kestra

[Kestra](https://kestra.io) schedules the lab's *jobs* — finite tasks like
`chezmoi update` and the Obsidian vault syncs. It is not a service manager:
long-running services stay in `compose/` and are converged by chezmoi's
deploy hook, exactly as before. Kestra replaces the *scheduling* half of
launchd, not the *supervision* half.

Flows are code: they live in `tofu/kestra/flows/` and are applied with
OpenTofu, not clicked into the UI. The UI is for watching runs, reading
logs, and re-running failures.

## How host jobs work

Kestra runs in a container and deliberately cannot touch the host — no
Docker socket, no host mounts beyond its own state. Host-side work leaves
through one audited door:

1. A dedicated ed25519 keypair (the `Kestra Job Bridge` 1Password item,
   materialized onto the mini by the bootstrap script — never in the repo).
2. `~/.ssh/authorized_keys` (chezmoi-managed) binds that key with
   `restrict,command="~/.local/bin/lab-job"` — no pty, no forwarding, and
   sshd runs the dispatcher *instead of* whatever was requested.
3. `lab-job` treats the requested command as a job *name* (strictly
   `[a-z0-9-]`) and resolves it to `jobs/<name>.sh` in this repo's checkout
   on the mini — the one `chezmoi update` already keeps fresh. Unknown or
   malformed names exit 64. The dispatcher itself never changes: **adding a
   job = one commit** carrying `jobs/<name>.sh` plus its flow in
   `tofu/kestra/flows/`.

So a compromised Kestra (or a leaked webhook key) can at worst run scripts
that were merged to main, never arbitrary commands.

## Secrets: 1Password is the origin

No secret is generated on the machine. You create them in the vault; the
bootstrap script materializes them into env files under `~/Docker/kestra/`
with `op read`. Wipe that directory and re-bootstrap and the *same* secrets
come back — recreating infrastructure can never lock you out, and there's
nothing to copy back into the vault afterwards.

Items (Personal vault — API Credential type unless noted; generate values
in 1Password, letters+digits to keep env files quote-free):

| Item | Fields | Notes |
| --- | --- | --- |
| `Kestra Admin` | `username` = trwolfe13@gmail.com, `credential` | UI login; also used by `tofu/kestra` |
| `Kestra Postgres` | `credential` | kestra ↔ postgres, never typed by a human |
| `Kestra Encryption Key` | `credential` | **exactly 32 chars** |
| `Kestra Job Bridge` | SSH Key item, ed25519 | 1Password generates it |

## First-time setup (on the mini, GUI session — `op` needs it)

1. Create the four 1Password items above.
2. `chezmoi apply` — bootstrap materializes `~/Docker/kestra/` (env files,
   SSH keypair) and `~/.docker-headless/config.json`.
3. `chezmoi apply` **again** — authorized_keys templates the public key in
   (it didn't exist during the first render).
4. The deploy hook brings the stack up; log in at http://macmini.local:8180
   with `Kestra Admin`.
5. Apply the flows: see `tofu/kestra/README.md`.

## Cutover from launchd

The `md.obsidian.headless-sync-*` launch agents run `ob sync --continuous`;
their Kestra replacements run one-shot syncs every 10 minutes instead
(trading ≤10 min of latency for logs, retries, and failure visibility).
Keep the overlap **short** — two syncers on one vault is its own risk.
Verify the first executions are green in the UI, then cut over the same day:

1. Delete both plists from `home/private_Library/private_LaunchAgents/`.
2. Add both targets to a `home/.chezmoiremove` — deleting a source file only
   makes chezmoi *stop managing* the target; `.chezmoiremove` is what makes
   apply delete it. (The launch-agent loader script won't clean up either:
   it only iterates plists still present in source.)
3. On the mini: `launchctl bootout gui/$(id -u)/md.obsidian.headless-sync-main`
   (and `-dnd`) — removing the file doesn't stop the already-loaded agent.
4. `chezmoi apply`.

## Upgrading

Bump the pinned tags, then `docker compose up -d`. Kestra: read the release
notes for migrations (the DB schema migrates forward automatically; there is
no downgrade). Postgres: never cross a major version without a dump/restore.

## Backup

Everything stateful is under `~/Docker/kestra/`. Cold backup: stop the
stack, copy the directory. The flows themselves need no backup — they're in
this repo.
