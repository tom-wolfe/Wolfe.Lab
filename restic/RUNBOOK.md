# restic runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap (once)

1. Create a Backblaze B2 account. Account → Application Keys → note the
   **master** key into 1P item `b2-master-key`; create the `restic-repo`
   Password item (generated, letters+digits).
2. Merge this slice; let the tick ship it. `brew "restic"` is in the
   Brewfile, so the chezmoi workflow (`install-packages.sh` on apply) installs it on every machine.
3. Apply the tofu root (from the mini or via `tofu-restic.yaml` once the
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

5. Run the backup workflow once from the Actions tab, then restic
   offsite (the first copy uploads everything — hours, once), then restic
   verify.
6. Verify the heartbeat: check `lab-restic-offsite` went green on
   healthchecks.io, then let it miss a night's grace once (disable the
   workflow) and confirm it pages.

## Restore

Find the snapshot, check the image it was taken under, restore like-for-like:

```sh
op run --env-file=restic/restic.env -- restic snapshots --tag service:forgejo
op run --env-file=restic/restic.env -- restic restore <id> --target /tmp/restore
```

The `image:` tag on every snapshot is the pin to restore onto — schema
migrates forward only, on every one of these services. Stop the stack,
put the restored directory where the service's compose file expects it,
start with the tagged image, then converge upward.

Disaster case (mini and Data2 both gone): `restic.env`'s repo path is
dead, but `offsite.env` works from any machine with restic, op and the
vault — restore from B2 directly. **Do an actual restore drill after
bootstrap**: restore forgejo's snapshot to /tmp, diff a few files against
the live tree, throw it away.
