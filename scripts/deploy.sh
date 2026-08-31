#!/bin/sh
# Converge one slice's compose stack — THE deploy implementation, shared by
# every lab.<slice>/deploy flow. One place to make deployment smarter;
# per-slice variation is data (the arguments) or a hook file, never a fork
# of this script.
#
#   deploy.sh <slice> [required-volume...]
#
# Runs INSIDE a docker CLI task container (image, socket and mounts come
# from the plugin defaults in kestra/application.yaml). `docker compose`
# here creates SIBLING containers on the host daemon, so every bind source
# a compose file names must be a path the DAEMON can resolve — which is
# why the runner mounts the repo checkout at its host path, and why this
# script must never write anywhere. It also runs fine on the host itself,
# where the same invariants hold trivially.
set -eu

repo="$(cd "$(dirname "$0")/.." && pwd)"
slice="$1"
shift

# Drive guard, for slices that bind external volumes (jellyfin,
# qbittorrent): at boot, Docker restarts containers before macOS mounts
# the drives, and an unmounted /Volumes path is a shadow directory on the
# internal disk — converging onto it starts containers over empty roots
# (a jellyfin scan against an empty root PRUNES the library).
#
# `mount | grep` can't work here — a container sees only the daemon's
# view, where a shadow directory looks like any other. So each real drive
# carries a sentinel file at its root (touch /Volumes/DataN/.lab-volume,
# once, at the desk): present means the real volume is mounted, absent
# means we're looking at the shadow. Failing makes the tick-chained
# deploy the retry loop — red while a drive is missing, convergent once
# it's back. The backup scripts keep their host-side mount guard; this is
# the same idea from the other side of the VM boundary.
for vol in "$@"; do
  if [ ! -f "$vol/.lab-volume" ]; then
    echo "deploy: no sentinel at $vol/.lab-volume — drive unmounted (or the sentinel was never created); refusing to converge onto a shadow path" >&2
    exit 1
  fi
done

docker compose \
  --project-directory "$repo/$slice" \
  up -d --remove-orphans

# Per-slice follow-up, as a hook file rather than a script fork: caddy
# reloads routes (snippets arrive via the repo bind mount and don't change
# compose's config hash). The hook runs in this same container, so the
# docker CLI is available to it.
hook="$repo/$slice/flows/deploy/post.sh"
if [ -x "$hook" ]; then
  "$hook"
fi
