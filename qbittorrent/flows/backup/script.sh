#!/bin/bash
# Cold backup of qbittorrent: stop → tar config/ → start, keep the last
# 10. The stop is not a database concern — the state is config files and
# fastresume data, not SQLite — but qBittorrent rewrites fastresume
# continuously while running, and a live tar could catch it mid-write;
# the stack restarts in seconds. Invoked through the lab-job bridge as
# `qbittorrent/backup`, or by hand.
set -euo pipefail

# Docker Desktop's CLI lives in /usr/local/bin, which non-interactive SSH
# sessions don't have on PATH (path_helper only runs for login shells).
export PATH="$PATH:/usr/local/bin"

slice="$(cd "$(dirname "$0")/../.." && pwd)"
state="$HOME/Docker/qbittorrent"

# Backups land on the external drive — guard hard against it being absent:
# an unmounted /Volumes path on macOS is just a directory on the internal
# disk, so writing there would silently "succeed" onto the small SSD and be
# shadowed when the drive next mounts. Checked BEFORE stopping the stack.
vol="/Volumes/Data2"
if ! mount | grep -q " on $vol ("; then
  echo "backup: $vol is not mounted — refusing to write to the internal disk" >&2
  exit 1
fi
mkdir -p "$vol/backups/qbittorrent"
backup="$vol/backups/qbittorrent/qbittorrent-$(date +%Y%m%d-%H%M%S).tar.gz"

docker compose --project-directory "$slice" stop
# Whatever happens below, never leave the stack down.
trap 'docker compose --project-directory "$slice" start >/dev/null' EXIT

# Record the image the archive pairs with (restore like-for-like first;
# the config schema moves forward across versions).
docker inspect qbittorrent --format '{{.Config.Image}}' > "$backup.image.txt"

# config/ only — gluetun/ (server-list cache) is disposable by design.
tar -czf "$backup.partial" -C "$state" config

# Integrity guard: discard if a concurrent deploy restarted the stack
# mid-tar (see forgejo/flows/backup/script.sh for the design notes).
if [ "$(docker inspect -f '{{.State.Running}}' qbittorrent 2>/dev/null)" = "true" ]; then
  rm -f "$backup.partial" "$backup.image.txt"
  echo "backup: qbittorrent restarted mid-backup — archive discarded, rerun me" >&2
  exit 1
fi
mv "$backup.partial" "$backup"
echo "backup complete: $backup ($(du -h "$backup" | cut -f1))"

# Keep the last 10.
ls -1t "$vol/backups/qbittorrent"/qbittorrent-*.tar.gz | tail -n +11 | while read -r old; do
  rm -f "$old" "$old.image.txt"
done || true
