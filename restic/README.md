# restic

[restic](https://restic.net) is the lab's backup mechanism — versioning,
retention, dedup, compression, encryption and the offsite copy, one tool.
Decision 2026-09-01 (Tom's): it **replaces** the old scheme outright rather
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

With the live state on the machines, that's 3-2-1: three copies, two media,
one offsite.

**Per-service backups keep their stop windows.** The stop is the
load-bearing part of the old scripts — it's what makes SQLite/LMDB
snapshots consistent — and it stays. The implementation is ONE shared
pipeline, `scripts/backup.sh` (deploy.sh's pattern on the bridge side:
stop → `restic backup` → start, with the mount, repo and mid-backup
restart guards); per-slice variation is data, a `flows/backup/backup.conf`
beside each flow declaring what to snapshot. kestra is the one override —
its own `script.sh`, because a live `pg_dump` (no stop, snapshotted
uncompressed so nights dedup) is a different shape, not a variation.
No script keeps or prunes anything, because —

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
   (kestra's labelled dumps) is kept forever.

**Watching it:** every flow here is `alert: high`, so a red run pages via
`system/alert-failed`. The flow *silently not running* is covered the same
way the tick is: `lab.restic/heartbeat` pings healthchecks.io
(`lab-restic-offsite`, declared in `tofu/`) after every green offsite run —
that silence is the only backup signal that leaves the building. And
`lab.restic/verify` (Sundays) runs `restic check` on both repos, reading a
5% pack sample back from B2 — an unverified backup is a hope, not a backup.

## Flows

| Flow | When | What |
| --- | --- | --- |
| `lab.<service>/backup` ×6 | 02:20–03:35 nightly | stop → snapshot → start (kestra: live pg_dump), unchanged schedules |
| `lab.restic/offsite` | 04:35 nightly | copy to B2, then forget+prune both repos |
| `lab.restic/heartbeat` | on offsite SUCCESS | ping `lab-restic-offsite` |
| `lab.restic/verify` | Sun 05:05 | `restic check` both repos, 5% data sample from B2 |
| `lab.restic/plan` / `apply` | tick / manual | the tofu root, standard OpenTofu CD |

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

The env files beside this README (`restic.env`, `offsite.env`) are op://
references resolved at spawn by `op run` — the same unattended path
`scripts/plan.sh` already uses on every tick (service-account token on the
mini). The repository URL lives in the vault, not the repo, because its
region segment only exists once the B2 account does.

## Bootstrap (once)

1. Create a Backblaze B2 account. Account → Application Keys → note the
   **master** key into 1P item `b2-master-key`; create the `restic-repo`
   Password item (generated, letters+digits).
2. Merge this slice; let the tick ship it. `brew "restic"` is in the
   Brewfile, so `lab.chezmoi/packages` installs it on every machine.
3. Apply the tofu root (from the mini or via `lab.restic/apply` once the
   flows land): creates the bucket, the scoped key, the check. Then fill
   the `restic-b2` item: `username`/`credential` from
   `op run --env-file=secrets.env -- tofu output -raw restic_application_key_id`
   (and `…_key`), `repository` from the bucket name plus the S3 endpoint
   shown in the B2 UI.
4. Initialize the repos — local first, then B2 **with the same chunker
   parameters**, or copies between them re-chunk and dedup dies:

   ```sh
   op run --env-file=restic/restic.env  -- restic init
   op run --env-file=restic/offsite.env -- restic init --copy-chunker-params
   ```

5. Run each `lab.<service>/backup` once from the Kestra UI, then
   `lab.restic/offsite` (the first copy uploads everything — hours, once),
   then `lab.restic/verify`.
6. Verify the heartbeat: check `lab-restic-offsite` went green on
   healthchecks.io, then let it miss a night's grace once (pause the flow)
   and confirm it pages.

## Restore

Find the snapshot, check the image it was taken under, restore like-for-like:

```sh
op run --env-file=restic/restic.env -- restic snapshots --tag service:forgejo
op run --env-file=restic/restic.env -- restic restore <id> --target /tmp/restore
```

The `image:` tag on every snapshot is the pin to restore onto — schema
migrates forward only, on every one of these services. Stop the stack,
put the restored directory where the service's compose file expects it,
start with the tagged image, then converge upward. kestra is the
exception (a SQL dump, not files):

```sh
op run --env-file=restic/restic.env -- restic dump latest /kestra.sql | \
  docker exec -i kestra-db psql -U kestra -d kestra
```

Disaster case (mini and Data2 both gone): `restic.env`'s repo path is
dead, but `offsite.env` works from any machine with restic, op and the
vault — restore from B2 directly. **Do an actual restore drill after
bootstrap**: restore forgejo's snapshot to /tmp, diff a few files against
the live tree, throw it away.

## Cost

B2 is ~$6/TB/month. Service state is single-digit GB — pennies. The knob
that matters later is scope, not price: adding sources (the Google Drive
question, Immich) is adding paths to back up, not redesigning. Both are
explicitly out of scope for now (Tom, 2026-09-01: Drive content unsorted;
Immich lands only after this exists — ROADMAP).

## Transition

The old tarball farm in `/Volumes/Data2/backups/` is left in place as a
fallback. Once a restore drill has passed and a couple of weeks of
snapshots exist, delete it by hand — nothing writes there any more.
