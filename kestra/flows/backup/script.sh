#!/bin/bash
# Dump the kestra database to a dated, gzipped file on the backups drive.
# Runnable by hand, by scripts/upgrade.sh (which labels the dump), and —
# through the lab-job bridge as `kestra/backup` — by ./flow.yaml beside it
# (nightly, 03:20). That's safe where a deploy flow isn't: a dump never
# restarts the executor (see README.md "Why kestra has no deploy flow").
#
#   flows/backup/script.sh [label]
#
# The optional label lands in the filename (upgrade.sh passes e.g.
# `pre-v1.3.34`). Fails hard if kestra-db isn't running — a backup that
# silently skips is worse than a red execution; the warn-and-continue
# policy for upgrades lives in upgrade.sh, not here.
#
# Retention: the last 10 UNLABELLED (scheduled) dumps are kept; labelled
# dumps (upgrade.sh's `pre-<version>`) are never pruned — they're rare,
# small, and the thing you'll want when a migration goes sideways.
set -euo pipefail

# Docker Desktop's CLI lives in /usr/local/bin, which non-interactive SSH
# sessions don't have on PATH (path_helper only runs for login shells).
export PATH="$PATH:/usr/local/bin"

if [ "$(docker inspect -f '{{.State.Running}}' kestra-db 2>/dev/null)" != "true" ]; then
  echo "backup: kestra-db is not running — nothing to dump" >&2
  exit 1
fi

# Backups land on the external drive — guard hard against it being absent:
# an unmounted /Volumes path on macOS is just a directory on the internal
# disk, so writing there would silently "succeed" onto the small SSD and be
# shadowed when the drive next mounts. This also gates upgrades: upgrade.sh
# calls this first, so no drive = no pre-upgrade dump = no upgrade.
vol="/Volumes/Data2"
if ! mount | grep -q " on $vol ("; then
  echo "backup: $vol is not mounted — refusing to write to the internal disk" >&2
  exit 1
fi
mkdir -p "$vol/backups/kestra"

label="${1:+-$1}"
backup="$vol/backups/kestra/kestra-$(date +%Y%m%d-%H%M%S)$label.sql.gz"

# Dump to a .partial first: a `>` redirect creates its target before the
# command runs, so a failed pg_dump would otherwise leave a truncated husk
# that looks like a backup (same trap the bootstrap-kestra-secrets script
# documents). Only a completed dump gets the real name.
echo "backing up kestra db -> $backup"
if ! docker exec kestra-db pg_dump -U kestra -d kestra | gzip > "$backup.partial"; then
  rm -f "$backup.partial"
  echo "backup: pg_dump failed" >&2
  exit 1
fi
mv "$backup.partial" "$backup"
echo "backup complete: $backup ($(du -h "$backup" | cut -f1))"

# Prune: only the fixed-width unlabelled names — a label always appends
# `-<label>` after the timestamp, so labelled dumps never match this glob.
ls -1t "$vol/backups/kestra"/kestra-????????-??????.sql.gz 2>/dev/null \
  | tail -n +11 | xargs rm -f || true
