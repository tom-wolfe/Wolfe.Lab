#!/bin/sh
# Ping a healthchecks.io check by slug — the dead man's switch transport.
#
#   heartbeat.sh <slug>
#
# The project-wide ping key comes from the vault through the node's
# service account (the runner's env_file). Checks are declared in tofu
# (chezmoi/tofu/checks.tf, restic/tofu/main.tf); the slug is the name.
set -eu

curl -fsS -o /dev/null --max-time 20 \
  "https://hc-ping.com/$(op read -n 'op://Wolfe.Lab/healthchecks-ping-key/credential')/$1"
