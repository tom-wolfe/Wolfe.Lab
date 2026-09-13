#!/bin/sh
# Pushover, from a failed workflow step — the Actions counterpart of the
# Kestra system flow. One place for the transport; workflows pass a title.
#
#   alert.sh <title> [message]
#
# Credentials come from the vault through the node's service account (the
# runner's env_file) — Forgejo holds no secrets.
set -eu

curl -fsS -o /dev/null https://api.pushover.net/1/messages.json \
  --form-string "token=$(op read 'op://Wolfe.Lab/pushover/credential')" \
  --form-string "user=$(op read 'op://Wolfe.Lab/pushover/username')" \
  --form-string "title=$1" \
  --form-string "message=${2:-}" \
  --form-string "priority=-1"
