#!/bin/bash
# Dump the kestra database into the restic repo — live, no downtime
# (pg_dump takes an MVCC-consistent snapshot), which is what makes a
# backup flow safe here where a deploy flow isn't (README.md "Why kestra
# has no deploy flow"). Runnable by hand, by scripts/upgrade.sh (which
# labels the dump), and — through the lab-job bridge as `kestra/backup` —
# by ./flow.yaml beside it (nightly, 03:20).
#
#   flows/backup/script.sh [label]
#
# The optional label becomes snapshot tags (upgrade.sh passes e.g.
# `pre-v1.3.34` → tags `pre-upgrade` + `label:pre-v1.3.34`). Fails hard
# if kestra-db isn't running — a backup that silently skips is worse
# than a red execution; the warn-and-continue policy for upgrades lives
# in upgrade.sh, not here.
#
# Retention lives in lab.restic/offsite, ONE policy for every service —
# except that anything tagged `pre-upgrade` is kept forever: rare,
# small, and the thing you'll want when a migration goes sideways.
set -euo pipefail

# Non-interactive SSH sessions miss path_helper: Docker Desktop's CLI is
# in /usr/local/bin, and restic + op come from Homebrew.
export PATH="$PATH:/usr/local/bin:/opt/homebrew/bin"

slice="$(cd "$(dirname "$0")/../.." && pwd)"
repo="$(dirname "$slice")"
env="$repo/restic/restic.env"

# The op service account, same fallback the tick uses: lab-job runs a
# non-login shell, so .zprofile's export never happened.
token_file="$HOME/Docker/1password/service-account-token"
if [ -z "${OP_SERVICE_ACCOUNT_TOKEN:-}" ] && [ -s "$token_file" ]; then
  OP_SERVICE_ACCOUNT_TOKEN=$(cat "$token_file")
  export OP_SERVICE_ACCOUNT_TOKEN
fi

if [ "$(docker inspect -f '{{.State.Running}}' kestra-db 2>/dev/null)" != "true" ]; then
  echo "backup: kestra-db is not running — nothing to dump" >&2
  exit 1
fi

# The restic repo lives on the external drive — an unmounted /Volumes
# path on macOS is just a directory on the internal disk. This also
# gates upgrades: upgrade.sh calls this first, so no drive = no
# pre-upgrade dump = no upgrade.
vol="/Volumes/Data2"
if ! mount | grep -q " on $vol ("; then
  echo "backup: $vol is not mounted — refusing to write to the internal disk" >&2
  exit 1
fi
if [ ! -f "$vol/restic/config" ]; then
  echo "backup: no restic repository at $vol/restic — see restic/README.md" >&2
  exit 1
fi

# The schema pairs with the APP image that wrote it — a dump restores
# cleanly onto the version that made it, not necessarily an older one.
image="$(docker inspect kestra --format '{{.Config.Image}}' 2>/dev/null || echo unknown)"

tags=(--tag "service:kestra" --tag "image:$image")
if [ -n "${1:-}" ]; then
  # A labelled dump is never pruned — `pre-upgrade` is what the retention
  # policy's --keep-tag matches; the label itself rides along for humans.
  tags+=(--tag pre-upgrade --tag "label:$1")
fi

# Dump to a temp file FIRST, then snapshot it via --stdin: piping
# pg_dump straight into restic would save a truncated-but-valid-looking
# snapshot if pg_dump died mid-stream (the same trap the old .partial
# dance guarded against). A failed dump exits here, before restic runs.
tmp="$(mktemp /tmp/kestra-dump.XXXXXX)"
trap 'rm -f "$tmp"' EXIT

echo "dumping kestra db"
if ! docker exec kestra-db pg_dump -U kestra -d kestra > "$tmp"; then
  echo "backup: pg_dump failed" >&2
  exit 1
fi

# Plain SQL, no gzip — restic compresses (zstd) and, more importantly,
# dedups the mostly-unchanged dump text against previous nights, which
# gzip would scramble.
op run --env-file="$env" -- restic backup --stdin --stdin-filename kestra.sql \
  "${tags[@]}" < "$tmp"

echo "backup complete: kestra.sql ($(du -h "$tmp" | cut -f1) dumped)"
