#!/bin/bash
# Runs `tofu plan` for a given slice.

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

# A provider cache survives the disposable checkout (runner env).
[ -z "${TF_PLUGIN_CACHE_DIR:-}" ] || mkdir -p "$TF_PLUGIN_CACHE_DIR"

# ONE op invocation, not two for rate limiting reasons.
exec op run --env-file=secrets.env -- sh -c \
  'tofu init -input=false >/dev/null && exec tofu plan -input=false -no-color -detailed-exitcode'
