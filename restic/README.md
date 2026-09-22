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
Photos and videos taken on a phone cannot, and nor can the personal
files (`files/`), so both are in and ride the offsite copy at a couple
of pounds a month.

**Per-service backups keep their stop windows.** The stop is the
load-bearing part of the old scripts — it's what makes SQLite/LMDB
snapshots consistent — and it stays. The implementation is ONE shared
pipeline: `lab backup` in `build/` (stop → `restic backup` → start, with
the mount, repo and mid-backup restart guards). Per-slice variation is
data — the `backup` section of the slice's `ritten.json` — declaring
what to snapshot: the paths, the excludes, the container to stop (or
none for a warm snapshot), and the container whose image tags it. No job keeps or prunes anything, because —

**Retention lives in ONE place:** `lab offsite` (`build/`, settings in
`ritten.json`), nightly at 04:35, after every backup has finished:

1. `restic copy` — ship every snapshot B2 doesn't have. This is
   *idempotent catch-up*, not a timed hand-off: a missed night ships on
   the next run, and a failed service backup just means one less snapshot
   to copy. No step here depends on another step's timing.
2. `restic forget --prune` with the policy in `ritten.json` (7 daily, 5
   weekly, 12 monthly, `pre-upgrade` forever) — the same policy applied
   to both repos, after the copy so nothing is pruned before it's offsite.
   Snapshots group by path (per service) automatically; `pre-upgrade`
   (labelled dumps taken before upgrades) is kept forever.

**Watching it:** every workflow here alerts on failure (Pushover). The
job *silently not running* is covered the way the lab's heartbeat is: the
offsite job's last step pings healthchecks.io (`lab-restic-offsite`,
declared in `tofu/`) after a green copy — that silence is the only backup
signal that leaves the building. And the verify workflow (Sundays) runs
`restic check` on both repos, reading a 5% pack sample back from B2 — an
unverified backup is a hope, not a backup. Each slice's own `restore-drill`
job runs every night, straight after its backup: the
slice's `backup.verify` paths — the files it needs to boot — come back
from the snapshot just taken into a scratch directory and are asserted
non-empty, so a snapshot that cannot be restored from is found the
same night. This slice checks the repositories; what a slice's snapshot
must hold is that slice's to say.

## From a secondary node

The backups drive hangs off the mini, and the pipeline does not change
shape for a node that doesn't have it: `lab backup` runs on the
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
| `sftp.env` | this directory | `restic.env`'s twin: the same password, the repository as an `sftp:` URL. `lab backup` picks it off macOS |

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

A Linux slice's backup is its own `<slice>-backup.yaml` running on its node;
until the first stateful slice lands on the Pi, the path is proven by
hand (`RUNBOOK.md` "A Linux node").

## Workflows

| Workflow | When | What |
| --- | --- | --- |
| `<slice>-backup.yaml` | nightly, 02:20 to 03:30, one slice each | `lab backup` then `lab restore-drill`: stop → snapshot → start, then the snapshot restored to scratch and asserted |
| `restic-offsite.yaml` | 04:35 nightly | `lab offsite`: copy to B2, forget+prune both repos, then ping `lab-restic-offsite` |
| `restic-verify.yaml` | Sun 05:05 | `lab verify` here: `restic check` both repos, 5% data sample from B2 |
| `restic-tofu.yaml` | push / daily | the tofu root, standard OpenTofu CD |

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
`sftp.env`) are op:// references, resolved at spawn by the CLI's
secrets provider — the same path every job in the lab uses. The
repository URL lives in the vault, not the repo, because its region
segment only exists once the B2 account does.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
