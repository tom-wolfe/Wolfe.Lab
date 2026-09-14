#!/bin/bash
# Runs `tofu plan` for a given slice.

set -euo pipefail

repo="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo/$1/tofu"

# A provider cache survives the disposable checkout (runner env).
[ -z "${TF_PLUGIN_CACHE_DIR:-}" ] || mkdir -p "$TF_PLUGIN_CACHE_DIR"

# The shared state backend under the root's own secrets, resolved ONCE for
# init and plan together.
exec "$repo/scripts/secrets.sh" run \
  --env-file "$repo/scripts/tofu-state.env" --env-file secrets.env -- sh -c \
  'tofu init -input=false >/dev/null && exec tofu plan -input=false -no-color -detailed-exitcode'
