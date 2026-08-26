#!/bin/bash
# One-time Garage cluster layout — the only storage setup that must happen
# before the admin API is usable. Buckets/keys/grants live in tofu (this
# slice's tofu/ seeds the state store; the rest are ordinary resources in
# whichever project needs them).
#
# Formerly the run_once_after_init-garage-layout chezmoi script; it's an
# action, so it lives with the other imperative bring-up steps now —
# setup.sh invokes it right after starting the stack. Idempotent: a no-op
# once a layout version exists.
set -uo pipefail

# Docker Desktop's CLI lives in /usr/local/bin, which non-interactive SSH
# sessions don't have on PATH (path_helper only runs for login shells).
export PATH="$PATH:/usr/local/bin"

ZONE="home"
CAPACITY="500G"

garage() { docker exec garage /garage "$@"; }

# Wait for the container — setup.sh has only just started it
ready=false
for _ in $(seq 1 30); do
  if garage status >/dev/null 2>&1; then ready=true; break; fi
  sleep 2
done
if ! $ready; then
  echo "ERROR: garage is not responding - is the stack up?" >&2
  exit 1
fi

version=$(garage layout show 2>/dev/null | sed -n 's/^Current cluster layout version: //p')
if [ "$version" = "0" ]; then
  node_id=$(garage node id -q 2>/dev/null | cut -c1-16)
  echo "applying initial layout: node $node_id, zone $ZONE, capacity $CAPACITY"
  garage layout assign -z "$ZONE" -c "$CAPACITY" "$node_id" >/dev/null
  garage layout apply --version 1 >/dev/null
elif [ -z "$version" ]; then
  echo "ERROR: could not parse 'garage layout show' output" >&2
  exit 1
else
  echo "layout already applied (version $version)"
fi
