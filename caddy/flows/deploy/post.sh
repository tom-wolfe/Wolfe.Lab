#!/bin/sh
# Deploy hook, run by scripts/deploy.sh after `up -d`.
set -eu

for f in "$LAB_REPO_DIR"/*/caddy.caddyfile; do
  slice="$(basename "$(dirname "$f")")"
  mkdir -p "$LAB_ROOT/$slice"
  cp "$f" "$LAB_ROOT/$slice/caddy.caddyfile"
done

docker exec caddy caddy reload --config /etc/caddy/lab/caddy/Caddyfile
