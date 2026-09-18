# restic

[restic](https://restic.net) is the lab's backup mechanism — versioning,
retention, dedup, compression, encryption and the offsite copy, one tool.
It **replaces** the old scheme outright rather
than layering over it. The previous design — ten full `.tar.gz` per service
on Data2, with a restic stage proposed on top — was backups chained on
backups, ~10× duplicated data, and would have needed the tar scripts
contorted (dropping gzip) so restic could undo their duplication. When
layer B has to reshape layer A's output to deduplicate it, layer B should
just replace layer A.

Not a service: no compose stack, nothing to deploy. Like `dns/`, the slice
is a tofu root (the offsite bucket, key and dead man's switch) plus flows.

## The design

**Two repositories, one password:**

| Repo | Where | Role |
| --- | --- | --- |
| `/Volumes/Data2/restic` | the backups drive | every backup lands here, at disk speed; fast local restores |
| `s3:https://s3.<region>.backblazeb2.com/wolfe-lab-restic` | Backblaze B2 | the offsite copy — the one that survives the enclosure, theft, fire |

Every node writes into the first one. The mini has the drive, and other nodes reach the same repository over SFTP (below).

With the live state on the machines, that's 3-2-1: three copies, two media,
one offsite.

**Media splits on whether it can be re-acquired.** Films and television
can, so they stay out of restic and a lost drive is a re-download.
Photos and videos taken on a phone cannot, so the Immich library is in,
and rides the offsite copy at a pound or so a month.

**Per-service backups keep their stop windows.** The stop is the
load-bearing part of the old scripts — it's what makes SQLite/LMDB
snapshots consistent — and it stays. The implementation is ONE shared
pipeline, `scripts/backup.sh` (deploy.sh's pattern:
stop → `restic backup` → start, with the mount, repo and mid-backup
restart guards); per-slice variation is data, a `flows/backup/backup.conf`
per slice declaring what to snapshot. No script keeps or prunes
anything, because —

**Retention lives in ONE place:** `flows/offsite/script.sh`, nightly at
04:35, after every backup has finished:

1. `restic copy` — ship every snapshot B2 doesn't have. This is
   *idempotent catch-up*, not a timed hand-off: a missed night ships on
   the next run, and a failed service backup just means one less snapshot
   to copy. No step here depends on another step's timing.
2. `restic forget --keep-daily 7 --keep-weekly 5 --keep-monthly 12
   --keep-tag pre-upgrade --prune` — the same policy applied to both
   repos, after the copy so nothing is pruned before it's offsite.
   Snapshots group by path (per service) automatically; `pre-upgrade`
   (labelled dumps taken before upgrades) is kept forever.

**Watching it:** every workflow here alerts on failure (Pushover). The
job *silently not running* is covered the way the lab's heartbeat is: the
offsite workflow's last step pings healthchecks.io (`lab-restic-offsite`,
declared in `tofu/`) after a green copy — that silence is the only backup
signal that leaves the building. And the verify workflow (Sundays) runs
`restic check` on both repos, reading a 5% pack sample back from B2 — an
unverified backup is a hope, not a backup.

## From a secondary node

The backups drive hangs off the mini, and the pipeline does not change
shape for a node that doesn't have it: `scripts/backup.sh` runs on the
node the slice lives on (`runs-on` is placement, as for deploys), stops
the stack there, and writes into the **same** repository — over SFTP to
the mini, which is restic's `sftp:` backend: restic runs `ssh`, and
`sftp-server` on the far side speaks the repository's file protocol.
One repository means offsite, retention and verify are untouched; the
Pi's snapshots are grouped by host and path like everything else.

What a Linux node needs, all of it chezmoi's (`chezmoi/home/`):

| Piece | Where | Note |
| --- | --- | --- |
| `restic` | `~/.local/bin`, pinned in `.chezmoiexternal.toml.tmpl` | the Macs get it from the Brewfile |
| `~/.ssh/restic` | `private_dot_ssh/create_private_restic.tmpl`, from the vault | SSH Key item `restic-sftp-<node>`, generated in the vault, written once |
| `Host macmini.tailf823b8.ts.net` | `private_dot_ssh/config.tmpl`, the `pi-node` block | user, the key, `accept-new` — the tailnet already authenticates the peer, and a first contact must not block a non-interactive job |
| the mini's `authorized_keys` line | `private_dot_ssh/private_authorized_keys.tmpl`, the `macmini-node` block | `restrict,command="/usr/libexec/sftp-server"` — the key cannot open a shell, forward a port or run anything else; the public half is read from the same vault item |
| `sftp.env` | this directory | `restic.env`'s twin: the same password, the repository as an `sftp:` URL. `backup.sh` picks it on Linux |

**What the key can do,** plainly: sftp-server runs as the mini's user, so
the key reads and writes what that user can — not just the repository.
The restriction is on *how* (file transfer only), not *where*; a chroot
would need `sshd_config` and a root-owned jail. Accepted: the Pi already
holds the vault token that reads the B2 credentials, so it can already
reach every byte of every backup, and the point of the forced command is
that a key is not a shell.

**Why not a REST server on the mini.** `rest-server` in a container
would give an append-only mode and a path jail, and it would put Data2
behind the VM boundary that every macOS container fault in the changelog
comes from. sshd is native and the drive is native; when the platform
layer moves to Linux (ROADMAP.md) and the drive's host changes, revisit.

A Linux slice's backup is a job in `backup.yaml` that runs on its node;
until the first stateful slice lands on the Pi, the path is proven by
hand (`RUNBOOK.md` "A Linux node").

## Workflows

| Workflow | When | What |
| --- | --- | --- |
| `backup.yaml` (matrix ×7) | 02:20 nightly, one slice after another | stop → snapshot → start |
| `restic-offsite.yaml` | 04:35 nightly | copy to B2, forget+prune both repos, then ping `lab-restic-offsite` |
| `restic-verify.yaml` | Sun 05:05 | `restic check` both repos, 5% data sample from B2 |
| `tofu-restic.yaml` | push / daily | the tofu root, standard OpenTofu CD |

Locking: backups take shared locks and may overlap each other safely;
`forget --prune` needs an exclusive lock, which is why offsite sits an
hour clear of the backup window. If a first-seed copy overruns into
Sunday's verify, the verify fails on the lock — rerun it.

## Secrets (1Password, Wolfe.Lab vault)

| Item | Type | Fields |
| --- | --- | --- |
| `restic-repo` | Password | `password` — the repo encryption password, both repos. **Losing this loses every backup**; it exists only in the vault |
| `restic-b2` | API Credential | `username` = scoped keyID, `credential` = scoped key (both from `tofu output` after apply), plus a custom `repository` field = the full `s3:https://…/wolfe-lab-restic` URL |
| `b2-master-key` | API Credential | `username` = master keyID, `credential` = master key — used only by `tofu/` to mint the scoped key |

The env files beside this README (`restic.env`, `offsite.env`,
`sftp.env`) are op:// references, resolved at spawn by
`scripts/secrets.sh` — the same path every job in the lab uses. The
repository URL lives in the vault, not the repo, because its region
segment only exists once the B2 account does.

## Cost

B2 is ~$6/TB/month. Service state is single-digit GB — pennies. The knob
that matters later is scope, not price: adding sources (the Google Drive
question, Immich) is adding paths to back up, not redesigning. Both are
explicitly out of scope for now (Drive content unsorted;
Immich lands only after this exists — ROADMAP).

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
