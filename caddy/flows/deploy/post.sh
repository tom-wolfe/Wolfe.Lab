#!/bin/sh
# Deploy hook, run by scripts/deploy.sh after `up -d`.
set -eu
docker exec caddy caddy reload --config /etc/caddy/Caddyfile
