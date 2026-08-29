#!/bin/bash
# Cold backup of jellyfin's state — config, database, library roots,
# plugins: the parts you can't re-fetch. Skips metadata/ by default
# (~670 MB of TMDB artwork that a "Refresh Metadata" re-downloads; pass
# --with-metadata to include it), plus cache/, log/, transcodes/ and the
# dead pre-10.11 library.db* files (see README "Pre-existing issues").
# The stop is not optional — a live SQLite file copied mid-write can be
# inconsistent — so expect ~30s of downtime. Invoked through the lab-job
# bridge as `jellyfin/backup`, or by hand.
set -euo pipefail

# Docker Desktop's CLI lives in /usr/local/bin, which non-interactive SSH
# sessions don't have on PATH (path_helper only runs for login shells).
export PATH="$PATH:/usr/local/bin"

slice="$(cd "$(dirname "$0")/../.." && pwd)"
# State stays where the native app left it — see README "Why the paths
# look strange". NOT ~/Docker like the other stacks.
state="$HOME/Library/Application Support"

# Backups land on the external drive — guard hard against it being absent:
# an unmounted /Volumes path on macOS is just a directory on the internal
# disk, so writing there would silently "succeed" onto the small SSD and be
# shadowed when the drive next mounts. Checked BEFORE stopping the stack.
vol="/Volumes/Data2"
if ! mount | grep -q " on $vol ("; then
  echo "backup: $vol is not mounted — refusing to write to the internal disk" >&2
  exit 1
fi
mkdir -p "$vol/backups/jellyfin"
backup="$vol/backups/jellyfin/jellyfin-$(date +%Y%m%d-%H%M%S).tar.gz"

# bsdtar prunes excluded directories — nested contents never get visited.
excludes=(
  --exclude 'jellyfin/cache'
  --exclude 'jellyfin/log'
  --exclude 'jellyfin/transcodes'
  # Dead since the 10.10 -> 10.11 migration; 10.11 reads jellyfin.db only.
  --exclude 'jellyfin/data/library.db.old'
  --exclude 'jellyfin/data/library.db-wal'
  --exclude 'jellyfin/data/library.db-shm'
)
if [ "${1:-}" != "--with-metadata" ]; then
  excludes+=(--exclude 'jellyfin/metadata')
fi

docker-compose --project-directory "$slice" stop
# Whatever happens below, never leave the stack down.
trap 'docker-compose --project-directory "$slice" start >/dev/null' EXIT

# Record the image the backup was taken with — restoring onto a newer
# image can fail on migrated schema (see README "Restore").
docker inspect jellyfin --format '{{.Config.Image}}' > "$backup.image.txt"

tar -czf "$backup.partial" "${excludes[@]}" -C "$state" jellyfin

# Integrity guard: discard if a concurrent deploy restarted the stack
# mid-tar (see forgejo/flows/backup/script.sh for the design notes).
if [ "$(docker inspect -f '{{.State.Running}}' jellyfin 2>/dev/null)" = "true" ]; then
  rm -f "$backup.partial" "$backup.image.txt"
  echo "backup: jellyfin restarted mid-backup — archive discarded, rerun me" >&2
  exit 1
fi
mv "$backup.partial" "$backup"
echo "backup complete: $backup ($(du -h "$backup" | cut -f1))"

# Keep the last 10.
ls -1t "$vol/backups/jellyfin"/jellyfin-*.tar.gz | tail -n +11 | while read -r old; do
  rm -f "$old" "$old.image.txt"
done || true
