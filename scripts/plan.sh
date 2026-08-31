#!/bin/bash
# `tofu plan` for one slice's root — the plan half of OpenTofu CD
# (kestra/README.md), shared by every lab.<slice>/plan flow. Runs on the
# HOST: lab-job's shared-pipeline fallback resolves `<slice>/plan` here
# and passes the slice, so op and the provider caches live in one place
# and no per-slice script exists. Repo-versioned, so the tick ships
# changes — nothing to chezmoi-apply.
#
#   plan.sh <slice>
#
# -detailed-exitcode: 0 = in sync (a quiet green), 2 = pending changes,
# 1 = error. Both non-zero exits fail the task; the flow's allowFailure
# turns that into WARNING — a low-priority push with the plan in the
# logs.
set -euo pipefail

repo="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo/$1/tofu"

# The op service account, same fallback the tick uses: lab-job runs a
# non-login shell, so .zprofile's export never happened.
token_file="$HOME/Docker/1password/service-account-token"
if [ -z "${OP_SERVICE_ACCOUNT_TOKEN:-}" ] && [ -s "$token_file" ]; then
  OP_SERVICE_ACCOUNT_TOKEN=$(cat "$token_file")
  export OP_SERVICE_ACCOUNT_TOKEN
fi

# Convergent and quick once .terraform/ is populated; the first run per
# root downloads providers into the checkout (untracked, survives pulls).
op run --env-file=secrets.env -- tofu init -input=false >/dev/null

exec op run --env-file=secrets.env -- \
  tofu plan -input=false -no-color -detailed-exitcode
