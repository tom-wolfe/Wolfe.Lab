#!/bin/sh
# Ping a healthchecks.io check by slug — the dead man's switch transport.
#
#   heartbeat.sh <slug>
set -eu

repo="$(cd "$(dirname "$0")/.." && pwd)"

curl -fsS -o /dev/null --max-time 20 \
  "https://hc-ping.com/$("$repo/scripts/secrets.sh" read 'op://Wolfe.Lab/healthchecks-ping-key/credential')/$1"
