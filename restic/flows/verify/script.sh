#!/bin/bash
# Weekly integrity check of both restic repositories — an unverified
# backup is a hope, not a backup. Invoked through the lab-job bridge as
# `restic/verify` by ./flow.yaml beside it (Sundays, 05:05), or by hand.
#
# The local check is structural (index and tree consistency; it reads
# metadata, not every pack). The B2 check additionally downloads a 5%
# random sample of pack data and verifies it against the index — over a
# year that samples most of the repo, for pennies of download. The full
# `--read-data` drill, and the actual restore drill, are manual and
# documented in restic/README.md.
set -euo pipefail

# Non-interactive SSH sessions miss path_helper: restic and op come from
# Homebrew.
export PATH="$PATH:/usr/local/bin:/opt/homebrew/bin"

slice="$(cd "$(dirname "$0")/../.." && pwd)"

# The op service account, same fallback the tick uses: lab-job runs a
# non-login shell, so .zprofile's export never happened.
token_file="$HOME/Docker/1password/service-account-token"
if [ -z "${OP_SERVICE_ACCOUNT_TOKEN:-}" ] && [ -s "$token_file" ]; then
  OP_SERVICE_ACCOUNT_TOKEN=$(cat "$token_file")
  export OP_SERVICE_ACCOUNT_TOKEN
fi

vol="/Volumes/Data2"
if ! mount | grep -q " on $vol ("; then
  echo "verify: $vol is not mounted — cannot check the local repo" >&2
  exit 1
fi
if [ ! -f "$vol/restic/config" ]; then
  echo "verify: no restic repository at $vol/restic — see restic/README.md Bootstrap" >&2
  exit 1
fi

echo "checking local repository"
op run --env-file="$slice/restic.env" -- restic check

echo "checking B2 repository (5% pack sample)"
op run --env-file="$slice/offsite.env" -- restic check --read-data-subset=5%

echo "verify complete"
