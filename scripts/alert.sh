#!/bin/sh
# Pushover, from a failed workflow step.
#
#   alert.sh <title> [message]
#
set -eu

repo="$(cd "$(dirname "$0")/.." && pwd)"
secrets="$repo/scripts/secrets.sh"
title="$1"
message="${2:-}"

if [ -z "$message" ] && [ -n "${GITHUB_RUN_NUMBER:-}" ]; then
  root="$(sed -n 's/^ *FORGEJO__server__ROOT_URL: *//p' "$repo/forgejo/compose.yaml" | tr -d '"' | sed 's:/*$::')"
  message="$root/${GITHUB_REPOSITORY:-tom-wolfe/Wolfe.Lab}/actions/runs/$GITHUB_RUN_NUMBER"
fi

curl -fsS -o /dev/null https://api.pushover.net/1/messages.json \
  --form-string "token=$("$secrets" read 'op://Wolfe.Lab/pushover/credential')" \
  --form-string "user=$("$secrets" read 'op://Wolfe.Lab/pushover/username')" \
  --form-string "title=$title" \
  --form-string "message=$message" \
  --form-string "priority=-1"
