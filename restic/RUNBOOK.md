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
   `scripts/secrets.sh run --env-file scripts/tofu-state.env --env-file restic/tofu/secrets.env -- tofu -chdir=restic/tofu output -raw restic_application_key_id`
   (and `…_key`), `repository` from the bucket name plus the S3 endpoint
   shown in the B2 UI.
4. Initialize the repos — local first, then B2 **with the same chunker
   parameters**, or copies between them re-chunk and dedup dies:

   ```sh
   scripts/secrets.sh run --env-file restic/restic.env  -- restic init
   scripts/secrets.sh run --env-file restic/offsite.env -- restic init --copy-chunker-params
   ```

5. Run the backup workflow once from the Actions tab, then restic
   offsite (the first copy uploads everything — hours, once), then restic
   verify.
6. Verify the heartbeat: check `lab-restic-offsite` went green on
   healthchecks.io, then let it miss a night's grace once (disable the
   workflow) and confirm it pages.

## A secondary node

Once per node, in order — the vault item must exist before the merge
that brings the key, or the `create_` template fails the whole apply.

1. **The key, in the vault:** generated there, never on a machine.

   ```sh
   op item create --vault Wolfe.Lab --category "SSH Key" \
     --title restic-sftp-wolfe-pi5 --ssh-generate-key ed25519
   ```

2. **Merge.** The chezmoi workflow renders, on the mini, the sftp-only
   `authorized_keys` line (public half from the item) and, on the node,
   `~/.ssh/restic`, the `Host` block and the restic binary.
3. **Prove it, from the node:**

   ```sh
   lab=~/.local/share/chezmoi   # the repo on the node
   $lab/scripts/secrets.sh run --env-file $lab/restic/sftp.env -- restic snapshots --latest 1
   ```

   The first contact accepts the mini's host key (`accept-new`); the
   listing proves key, forced command, path and password. Then the write
   path, with something small and disposable:

   ```sh
   $lab/scripts/secrets.sh run --env-file $lab/restic/sftp.env -- restic backup ~/.local/share/Wolfe.Lab --tag drill
   ```

   and from the mini, forget it by ID (`forget` needs an ID or a policy;
   a tag alone selects nothing) — a lone snapshot in its own group is
   otherwise kept by the retention policy forever:

   ```sh
   scripts/secrets.sh run --env-file restic/restic.env -- restic snapshots --tag drill
   scripts/secrets.sh run --env-file restic/restic.env -- restic forget <id> --prune
   ```

Rotation: delete the vault item and recreate it (step 1), `rm
~/.ssh/restic` on the node, and let the chezmoi workflow — or `chezmoi
apply` on both machines — render both halves again.

## Restore

Find the snapshot, check the image it was taken under, restore like-for-like:

```sh
scripts/secrets.sh run --env-file restic/restic.env -- restic snapshots --tag service:forgejo
scripts/secrets.sh run --env-file restic/restic.env -- restic restore <id> --target /tmp/restore
```

The `image:` tag on every snapshot is the pin to restore onto — schema
migrates forward only, on every one of these services. Stop the stack,
put the restored directory where the service's compose file expects it,
start with the tagged image, then converge upward.

From a secondary node, the same commands with `restic/sftp.env`; the restored
tree lands on the node, and `docker compose` there.

Disaster case (mini and Data2 both gone): `restic.env`'s repo path is
dead, but `offsite.env` works from any machine with restic, op and the
vault — restore from B2 directly. **Do an actual restore drill after
bootstrap**: restore forgejo's snapshot to /tmp, diff a few files against
the live tree, throw it away.
