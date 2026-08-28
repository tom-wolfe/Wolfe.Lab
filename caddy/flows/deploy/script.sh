#!/bin/bash
# Converge this slice's compose stack. Invoked through the lab-job bridge as
# `caddy/deploy` by ./flow.yaml beside it. Safe to run any time: `up -d` is
# convergent, and the reload below is a no-op when nothing changed.
set -euo pipefail

# Standalone docker-compose, not `docker compose` — see jellyfin's script
# for why (the plugin form doesn't resolve in headless sessions).
docker-compose \
  --project-directory "$(cd "$(dirname "$0")/../.." && pwd)" \
  up -d --remove-orphans

# Route snippets (<slice>/caddy.caddyfile) arrive via the repo bind mount,
# so `up -d` sees no config-hash change when only THEY changed. Reload
# explicitly — graceful, zero-downtime, no-op when config is unchanged.
docker exec caddy caddy reload --config /etc/caddy/Caddyfile
