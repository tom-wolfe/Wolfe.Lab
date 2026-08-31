#!/bin/bash
# `tofu apply` for one slice's root — the apply half of OpenTofu CD
# (kestra/README.md), shared by every lab.<slice>/apply flow, resolved
# via lab-job's shared-pipeline fallback like plan.sh beside it.
#
#   apply.sh <slice>
#
# -auto-approve, because the approval already happened: a human triggered
# the flow after reading lab.<slice>/plan's output — or the root is
# kestra's, which auto-applies by decision. Nothing invokes this for
# garage: that root is manual by design (its disposable state seeds the
# backend the others stand on).
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

op run --env-file=secrets.env -- tofu init -input=false >/dev/null

exec op run --env-file=secrets.env -- \
  tofu apply -input=false -no-color -auto-approve
