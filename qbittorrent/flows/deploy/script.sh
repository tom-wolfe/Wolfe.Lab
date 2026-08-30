#!/bin/bash
# Converge this slice's compose stack. Invoked through the lab-job bridge as
# `qbittorrent/deploy` by ./flow.yaml beside it, which chains on every green
# chezmoi tick. Safe to run any time: `up -d` is convergent — compose's own
# com.docker.compose.config-hash labels make unchanged services no-ops.
set -euo pipefail

# Standalone docker-compose, not `docker compose` — see jellyfin's script
# for why (the plugin form doesn't resolve in headless sessions).
exec docker-compose \
  --project-directory "$(cd "$(dirname "$0")/../.." && pwd)" \
  up -d --remove-orphans
