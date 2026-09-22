#!/bin/bash
# Fresh-server bring-up: the one imperative sequence, because Forgejo, which
# runs every other deploy, cannot deploy itself into existence. Every job is
# convergent, so re-runs are no-ops. Extra arguments reach every job:
# `./setup.sh --dry-run` rehearses the lot.
#
# Before: 1Password signed in, chezmoi applied, Docker Desktop running
# (RUNBOOK.md "New machine bootstrap"). After: RUNBOOK.md "After setup.sh".
set -euo pipefail

repo="$(cd "$(dirname "$0")" && pwd)"
extra=("$@")

lab() {
  local slice=$1
  shift
  echo "==> $slice $1"
  (cd "$repo/$slice" && dotnet run --project "$repo/build/src/Wolfe.Lab.Build" -- "$@" --auto-approve ${extra[@]+"${extra[@]}"})
}

lab caddy renew-certs
lab caddy deploy
lab garage deploy
lab garage init-layout
lab forgejo deploy
lab jellyfin deploy
lab sonarr deploy
lab radarr deploy
lab beszel deploy

echo
echo "Stacks are up. What remains is by hand: RUNBOOK.md \"After setup.sh\"."
