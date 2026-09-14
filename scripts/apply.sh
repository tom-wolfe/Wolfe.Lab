#!/bin/bash
# Runs `tofu apply` for a given slice.

set -euo pipefail

repo="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo/$1/tofu"

# A provider cache survives the disposable checkout (runner env).
[ -z "${TF_PLUGIN_CACHE_DIR:-}" ] || mkdir -p "$TF_PLUGIN_CACHE_DIR"

exec "$repo/scripts/secrets.sh" run \
  --env-file "$repo/scripts/tofu-state.env" --env-file secrets.env -- sh -c \
  'tofu init -input=false >/dev/null && exec tofu apply -input=false -no-color -auto-approve'
