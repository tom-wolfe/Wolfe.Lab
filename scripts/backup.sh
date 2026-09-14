#!/bin/bash
# Runs a cold backup for one slice
#
#   backup.sh <slice> [flag...]
#

set -euo pipefail

# A workflow step gets the runner's PATH; a hand run may not have brew's.
export PATH="$PATH:/usr/local/bin:/opt/homebrew/bin"

repo="$(cd "$(dirname "$0")/.." && pwd)"
# The stack is stopped and started where deploy.sh installed it.
root="${LAB_ROOT:-$HOME/.local/share/Wolfe.Lab}"
slice="${1:?usage: backup.sh <slice>}"
shift

# What to snapshot. The conf must set paths=(...); it may add restic
# excludes=(...) and override container= (defaults to the slice name).
# Sourced, so it sees this script's remaining arguments as $1….
conf="$repo/$slice/flows/backup/backup.conf"
if [ ! -f "$conf" ]; then
  echo "backup: no $conf — $slice doesn't use the backup pipeline" >&2
  exit 64
fi
container="$slice"
paths=()
excludes=()
# shellcheck disable=SC1090
source "$conf"
if [ "${#paths[@]}" -eq 0 ]; then
  echo "backup: $conf declares no paths" >&2
  exit 64
fi

# The op service account: a workflow step is a non-login shell, so
# .zprofile's export never happened (the runner's env_file usually has it).
token_file="$HOME/Docker/1password/service-account-token"
if [ -z "${OP_SERVICE_ACCOUNT_TOKEN:-}" ] && [ -s "$token_file" ]; then
  OP_SERVICE_ACCOUNT_TOKEN=$(cat "$token_file")
  export OP_SERVICE_ACCOUNT_TOKEN
fi

# The restic repo lives on the external drive — an unmounted /Volumes
# path on macOS is just a directory on the internal disk. The drive carries
# a sentinel file at its root (the same guard deploy.sh uses: touch
# /Volumes/DataN/.lab-volume once, at the desk); check it, then that the
# repo exists, BEFORE stopping the stack.
vol="/Volumes/Data2"
if [ ! -f "$vol/.lab-volume" ]; then
  echo "backup: $vol is not mounted — refusing to write to the internal disk" >&2
  exit 1
fi
if [ ! -f "$vol/restic/config" ]; then
  echo "backup: no restic repository at $vol/restic — see restic/README.md" >&2
  exit 1
fi

docker compose --project-directory "$root/$slice" stop
# Whatever happens below, never leave the stack down.
trap 'docker compose --project-directory "$root/$slice" start >/dev/null' EXIT

# The image rides on the snapshot as a tag — these schemas migrate
# forward only, so a restore pairs with the version that wrote it (each
# slice's RUNBOOK.md "Restore").
image="$(docker inspect "$container" --format '{{.Config.Image}}')"

out="$(op run --env-file="$repo/restic/restic.env" -- restic backup \
  "${paths[@]}" ${excludes[@]+"${excludes[@]}"} \
  --tag "service:$slice" --tag "image:$image" 2>&1)" || {
  printf '%s\n' "$out"
  echo "backup: restic failed" >&2
  exit 1
}
printf '%s\n' "$out"
snap="$(printf '%s\n' "$out" | sed -n 's/^snapshot \([0-9a-f]*\) saved$/\1/p')"

# Integrity guard: if something restarted the stack mid-snapshot (a
# deploy flow converging concurrently), the copy may contain live
# mid-write files — discard rather than trust. `restic forget <id>`
# drops just that snapshot; the data it referenced is rewritten out by
# the nightly prune. Backups run serially in one job to make this rare.
if [ "$(docker inspect -f '{{.State.Running}}' "$container" 2>/dev/null)" = "true" ]; then
  if [ -n "$snap" ]; then
    op run --env-file="$repo/restic/restic.env" -- restic forget "$snap" >/dev/null
  fi
  echo "backup: $container restarted mid-backup — snapshot discarded, rerun me" >&2
  exit 1
fi
echo "backup complete: snapshot ${snap:-unknown}"
