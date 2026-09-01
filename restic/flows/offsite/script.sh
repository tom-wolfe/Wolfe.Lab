#!/bin/bash
# Ship the local restic repository offsite and apply retention — the
# second half of the backup design (restic/README.md). Invoked through
# the lab-job bridge as `restic/offsite` by ./flow.yaml beside it
# (nightly, 04:35, after every service backup has finished), or by hand.
#
# Three steps, in an order that matters:
#   1. copy   — every snapshot the B2 repo doesn't have yet. Idempotent
#               catch-up, not a timed hand-off: a night the copy missed is
#               shipped by the next run, and a failed service backup just
#               means there's one less snapshot to copy.
#   2. forget — the retention policy, applied to BOTH repos identically,
#               AFTER the copy so nothing is pruned before it's offsite.
#               This is the ONE place retention lives; the per-service
#               scripts keep nothing and prune nothing.
#   3. --prune rewrites what forget unreferenced, in the same pass.
#
# forget --prune takes an EXCLUSIVE repo lock; the schedule keeps this
# clear of the 02:20–03:35 backup window so it never contends.
set -euo pipefail

# Non-interactive SSH sessions miss path_helper: Docker Desktop's CLI is
# in /usr/local/bin, and restic + op come from Homebrew.
export PATH="$PATH:/usr/local/bin:/opt/homebrew/bin"

slice="$(cd "$(dirname "$0")/../.." && pwd)"

# The op service account, same fallback the tick uses: lab-job runs a
# non-login shell, so .zprofile's export never happened.
token_file="$HOME/Docker/1password/service-account-token"
if [ -z "${OP_SERVICE_ACCOUNT_TOKEN:-}" ] && [ -s "$token_file" ]; then
  OP_SERVICE_ACCOUNT_TOKEN=$(cat "$token_file")
  export OP_SERVICE_ACCOUNT_TOKEN
fi

# The local repo lives on the external drive — an unmounted /Volumes path
# on macOS is just a directory on the internal disk, so check the mount,
# then that the repo actually exists (bootstrap is manual: README.md).
vol="/Volumes/Data2"
if ! mount | grep -q " on $vol ("; then
  echo "offsite: $vol is not mounted — nothing to copy from" >&2
  exit 1
fi
if [ ! -f "$vol/restic/config" ]; then
  echo "offsite: no restic repository at $vol/restic — see restic/README.md Bootstrap" >&2
  exit 1
fi

echo "copying new snapshots to B2"
op run --env-file="$slice/offsite.env" -- restic copy

# One policy, both repos. Nightlies thin out with age; anything tagged
# pre-upgrade (kestra's labelled dumps) is kept forever — rare, small,
# and the thing you want when a migration goes sideways.
policy=(--keep-daily 7 --keep-weekly 5 --keep-monthly 12 --keep-tag pre-upgrade)

echo "applying retention locally"
op run --env-file="$slice/restic.env" -- restic forget "${policy[@]}" --prune

echo "applying retention on B2"
op run --env-file="$slice/offsite.env" -- restic forget "${policy[@]}" --prune

echo "offsite complete"
