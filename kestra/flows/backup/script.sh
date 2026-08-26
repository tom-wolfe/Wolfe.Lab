#!/bin/bash
# Dump the kestra database to a dated, gzipped file in the state directory.
# Runnable by hand, by scripts/upgrade.sh (which labels the dump), and —
# through the lab-job bridge as `kestra/backup` — by a future scheduled
# backup flow. That's safe where a deploy flow isn't: a dump never restarts
# the executor (see README.md "Why kestra has no deploy flow").
#
#   flows/backup/script.sh [label]
#
# The optional label lands in the filename (upgrade.sh passes e.g.
# `pre-v1.3.34`). Fails hard if kestra-db isn't running — a backup that
# silently skips is worse than a red execution; the warn-and-continue
# policy for upgrades lives in upgrade.sh, not here.
#
# Retention: deliberately none yet. Decide when this gets a schedule — and
# the pre-upgrade dumps should probably survive any pruning.
set -euo pipefail

# Docker Desktop's CLI lives in /usr/local/bin, which non-interactive SSH
# sessions don't have on PATH (path_helper only runs for login shells).
export PATH="$PATH:/usr/local/bin"

if [ "$(docker inspect -f '{{.State.Running}}' kestra-db 2>/dev/null)" != "true" ]; then
  echo "backup: kestra-db is not running — nothing to dump" >&2
  exit 1
fi

state="${KESTRA_STATE:-$HOME/Docker/kestra}"
mkdir -p "$state/backups"

label="${1:+-$1}"
backup="$state/backups/kestra-$(date +%Y%m%d-%H%M%S)$label.sql.gz"

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
