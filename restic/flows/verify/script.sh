#!/bin/bash
# Weekly integrity check of both restic repositories — an unverified
# backup is a hope, not a backup. Run Sundays at 05:05 by
# .forgejo/workflows/restic-verify.yaml, or by hand.
#
# The local check is structural (index and tree consistency; it reads
# metadata, not every pack). The B2 check additionally downloads a 5%
# random sample of pack data and verifies it against the index — over a
# year that samples most of the repo, for pennies of download. The full
# `--read-data` drill, and the actual restore drill, are manual and
# documented in restic/README.md.
set -euo pipefail

# A hand run may lack brew's PATH; a workflow step has the runner's.
export PATH="$PATH:/usr/local/bin:/opt/homebrew/bin"

slice="$(cd "$(dirname "$0")/../.." && pwd)"
secrets="$slice/../scripts/secrets.sh"

vol="/Volumes/Data2"
if [ ! -f "$vol/.lab-volume" ]; then
  echo "verify: $vol is not mounted — cannot check the local repo" >&2
  exit 1
fi
if [ ! -f "$vol/restic/config" ]; then
  echo "verify: no restic repository at $vol/restic — see restic/README.md Bootstrap" >&2
  exit 1
fi

echo "checking local repository"
"$secrets" run --env-file "$slice/restic.env" -- restic check

echo "checking B2 repository (5% pack sample)"
"$secrets" run --env-file "$slice/offsite.env" -- restic check --read-data-subset=5%

echo "verify complete"
