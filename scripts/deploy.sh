#!/bin/sh
# Converge one slice's compose stack — THE deploy implementation, shared by
# every deploy workflow. One place to make deployment smarter; per-slice
# variation is data (the arguments) or a hook file, never a fork of this
# script.
#
#   deploy.sh <slice> [required-volume...]
#
# Runs on the node, from a checkout of the repo (the workflow's disposable
# workspace, or any clone by hand). Containers bind-mount files from the
# slice — config dirs, caddy snippets — and keep reading them after this
# job is gone, so the slice is first INSTALLED into $LAB_ROOT/<slice>
# (rsync, so a running container never sees its directory vanish) and
# compose runs from there. The checkout is never referenced by a container.
set -eu

repo="$(cd "$(dirname "$0")/.." && pwd)"
slice="$1"
shift
root="${LAB_ROOT:-$HOME/.local/share/Wolfe.Lab}"
release="$root/$slice"

# Drive guard, for slices that bind external volumes. At boot, Docker
# restarts containers before macOS mounts the drives. Each real drive
# carries a sentinel file at its root (touch /Volumes/DataN/.lab-volume,
# once, at the desk): present means the real volume is mounted, absent
# means we're looking at the shadow. Failing keeps the deploy red while a
# drive is missing; a re-run converges once it's back.
for vol in "$@"; do
  if [ ! -f "$vol/.lab-volume" ]; then
    echo "deploy: no sentinel at $vol/.lab-volume — drive unmounted (or the sentinel was never created); refusing to converge onto a shadow path" >&2
    exit 1
  fi
done

# Install: the slice minus what the node never runs (flows/ are job
# scripts and hooks, tofu/ is IaC). --delete so a removed config file is removed.
mkdir -p "$release"
rsync -a --delete --exclude flows/ --exclude tofu/ "$repo/$slice/" "$release/"

docker compose \
  --project-directory "$release" \
  up -d --remove-orphans

# Per-slice follow-up, as a hook file rather than a script fork (caddy
# gathers every slice's route snippet and reloads). Runs from the checkout,
# with the install location and the checkout root in its environment.
hook="$repo/$slice/flows/deploy/post.sh"
if [ -x "$hook" ]; then
  LAB_ROOT="$root" LAB_RELEASE="$release" LAB_REPO_DIR="$repo" "$hook"
fi
