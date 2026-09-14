#!/bin/sh
# Pushover, from a failed workflow step.
#
#   alert.sh <title> [message]
#
# Credentials come from the vault through the node's service account (the
# runner's env_file) — Forgejo holds no secrets.
set -eu

repo="$(cd "$(dirname "$0")/.." && pwd)"
title="$1"
message="${2:-}"

if [ -z "$message" ] && [ -n "${GITHUB_RUN_NUMBER:-}" ]; then
  root="$(sed -n 's/^ *FORGEJO__server__ROOT_URL: *//p' "$repo/forgejo/compose.yaml" | tr -d '"' | sed 's:/*$::')"
  message="$root/${GITHUB_REPOSITORY:-tom-wolfe/Wolfe.Lab}/actions/runs/$GITHUB_RUN_NUMBER"
fi

curl -fsS -o /dev/null https://api.pushover.net/1/messages.json \
  --form-string "token=$(op read 'op://Wolfe.Lab/pushover/credential')" \
  --form-string "user=$(op read 'op://Wolfe.Lab/pushover/username')" \
  --form-string "title=$title" \
  --form-string "message=$message" \
  --form-string "priority=-1"
