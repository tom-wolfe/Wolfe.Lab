#!/bin/bash
# Converge this slice's compose stack. Invoked through the lab-job bridge as
# `qbittorrent/deploy` by ./flow.yaml beside it, which chains on every green
# chezmoi tick. Safe to run any time: `up -d` is convergent — compose's own
# com.docker.compose.config-hash labels make unchanged services no-ops.
set -euo pipefail

# The media drives must be REAL mounts before converging: at boot, Docker
# restarts containers before macOS mounts the external drives, and an
# unmounted /Volumes path is just a directory on the internal disk. A
# container started against that either fails outright (the good outcome,
# observed at the 2026-08-30 reboot) or comes up over an empty shadow
# directory — and qbittorrent's fastresume paths all dangle,
# erroring every torrent it manages.
# Read-side twin of the mount guard in every backup script. Failing here
# makes the tick-chained deploy the retry loop: red (alerting) while a
# drive is missing, convergent again once it's back.
for vol in /Volumes/Data1 /Volumes/Data2; do
  if ! mount | grep -q " on $vol ("; then
    echo "deploy: $vol is not mounted — refusing to converge onto a shadow path" >&2
    exit 1
  fi
done

# Standalone docker-compose, not `docker compose` — see jellyfin's script
# for why (the plugin form doesn't resolve in headless sessions).
exec docker-compose \
  --project-directory "$(cd "$(dirname "$0")/../.." && pwd)" \
  up -d --remove-orphans
