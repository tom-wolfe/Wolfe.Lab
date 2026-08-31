#!/bin/bash
# Cold backup of the beszel hub: stop → tar data/ → start, keep the last
# 10. The stop is not optional — the hub is a PocketBase app on SQLite in
# WAL mode, and tarring that live can capture a database file without the
# -wal that completes it. Invoked through the lab-job bridge as
# `beszel/backup`, or by hand.
#
# The AGENT has nothing to back up: its whole configuration is
# ~/.config/beszel/beszel-agent.env, a chezmoi create_ template whose
# origin is 1Password (same reasoning as garage.env).
set -euo pipefail

# Docker Desktop's CLI lives in /usr/local/bin, which non-interactive SSH
# sessions don't have on PATH (path_helper only runs for login shells).
export PATH="$PATH:/usr/local/bin"

slice="$(cd "$(dirname "$0")/../.." && pwd)"
state="$HOME/Docker/beszel"

# Backups land on the external drive — guard hard against it being absent:
# an unmounted /Volumes path on macOS is just a directory on the internal
# disk, so writing there would silently "succeed" onto the small SSD and be
# shadowed when the drive next mounts. Checked BEFORE stopping the stack.
vol="/Volumes/Data2"
if ! mount | grep -q " on $vol ("; then
  echo "backup: $vol is not mounted — refusing to write to the internal disk" >&2
  exit 1
fi
mkdir -p "$vol/backups/beszel"
backup="$vol/backups/beszel/beszel-$(date +%Y%m%d-%H%M%S).tar.gz"

docker compose --project-directory "$slice" stop
# Whatever happens below, never leave the stack down.
trap 'docker compose --project-directory "$slice" start >/dev/null' EXIT

# Record the image — PocketBase migrates the schema forward on boot, so a
# restored archive has to be paired with the version that wrote it.
docker inspect beszel --format '{{.Config.Image}}' > "$backup.image.txt"

tar -czf "$backup.partial" -C "$state" data

# Integrity guard: discard if a concurrent deploy restarted the stack
# mid-tar (see forgejo/flows/backup/script.sh for the design notes).
if [ "$(docker inspect -f '{{.State.Running}}' beszel 2>/dev/null)" = "true" ]; then
  rm -f "$backup.partial" "$backup.image.txt"
  echo "backup: beszel restarted mid-backup — archive discarded, rerun me" >&2
  exit 1
fi
mv "$backup.partial" "$backup"
echo "backup complete: $backup ($(du -h "$backup" | cut -f1))"

# Keep the last 10.
ls -1t "$vol/backups/beszel"/beszel-*.tar.gz | tail -n +11 | while read -r old; do
  rm -f "$old" "$old.image.txt"
done || true
