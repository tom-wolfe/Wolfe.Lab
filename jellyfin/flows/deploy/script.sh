#!/bin/bash
# Converge this slice's compose stack. Invoked through the lab-job bridge as
# `jellyfin/deploy` by ./flow.yaml beside it, which chains on every green
# chezmoi tick. Safe to run any time: `up -d` is convergent — compose's own
# com.docker.compose.config-hash labels make unchanged services no-ops.
set -euo pipefail

# Standalone docker-compose (Homebrew), not `docker compose`: the plugin
# form resolves through $DOCKER_CONFIG/cli-plugins — which the headless
# config the job bridge uses deliberately doesn't have — never through
# PATH. The standalone binary is an ordinary PATH-resolved command that
# behaves the same in every session type.
exec docker-compose \
  --project-directory "$(cd "$(dirname "$0")/../.." && pwd)" \
  up -d --remove-orphans
