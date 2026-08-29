#!/bin/bash
# Cold backup of forgejo's data directory: stop → tar → start, keep the
# last 10. The stop is not optional — a live SQLite file copied mid-write
# can be inconsistent — so expect ~30s of downtime, which is why
# ./flow.yaml schedules this at night. Invoked through the lab-job bridge
# as `forgejo/backup`, or by hand.
set -euo pipefail

# Docker Desktop's CLI lives in /usr/local/bin, which non-interactive SSH
# sessions don't have on PATH (path_helper only runs for login shells).
export PATH="$PATH:/usr/local/bin"

slice="$(cd "$(dirname "$0")/../.." && pwd)"
data="${FORGEJO_DATA:-$HOME/Docker/forgejo/data}"
root="$(dirname "$data")"

# Backups land on the external drive — guard hard against it being absent:
# an unmounted /Volumes path on macOS is just a directory on the internal
# disk, so writing there would silently "succeed" onto the small SSD and be
# shadowed when the drive next mounts. Checked BEFORE stopping the stack.
vol="/Volumes/Data2"
if ! mount | grep -q " on $vol ("; then
  echo "backup: $vol is not mounted — refusing to write to the internal disk" >&2
  exit 1
fi
mkdir -p "$vol/backups/forgejo"
backup="$vol/backups/forgejo/forgejo-$(date +%Y%m%d-%H%M%S).tar.gz"

docker-compose --project-directory "$slice" stop
# Whatever happens below, never leave the stack down.
trap 'docker-compose --project-directory "$slice" start >/dev/null' EXIT

# Record the image the backup was taken with — restoring onto a newer
# image can fail on migrated schema (see README "Restore").
docker inspect forgejo --format '{{.Config.Image}}' > "$backup.image.txt"

tar -czf "$backup.partial" -C "$root" "$(basename "$data")"

# Integrity guard: if something restarted the stack mid-tar (a deploy flow
# converging concurrently), the copy may contain live SQLite writes —
# discard rather than trust. The schedule sits clear of the tick's
# quarter-hour columns to make this rare.
if [ "$(docker inspect -f '{{.State.Running}}' forgejo 2>/dev/null)" = "true" ]; then
  rm -f "$backup.partial" "$backup.image.txt"
  echo "backup: forgejo restarted mid-backup — archive discarded, rerun me" >&2
  exit 1
fi
mv "$backup.partial" "$backup"
echo "backup complete: $backup ($(du -h "$backup" | cut -f1))"

# Keep the last 10 (timestamped names sort chronologically).
ls -1t "$vol/backups/forgejo"/forgejo-*.tar.gz | tail -n +11 | while read -r old; do
  rm -f "$old" "$old.image.txt"
done || true
