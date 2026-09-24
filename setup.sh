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

# Compiles the checkout rather than running the pinned `lab`: on a fresh
# server the feed `lab` installs from is on the Forgejo this script is
# bringing up (build/README.md "How workflows run it").
lab() {
  local component=$1
  shift
  echo "==> $component $1"
  (cd "$repo/$component" && dotnet run --project "$repo/build/src/Wolfe.Lab.Build" -- "$@" --auto-approve ${extra[@]+"${extra[@]}"})
}

lab caddy/certs renew
lab caddy/compose deploy
lab caddy/routes deploy
lab garage/compose deploy
lab garage/layout init
lab forgejo/compose deploy
lab jellyfin/compose deploy
lab sonarr/compose deploy
lab radarr/compose deploy
lab paperless/compose deploy
lab beszel/compose deploy

echo
echo "Stacks are up. What remains is by hand: RUNBOOK.md \"After setup.sh\"."
