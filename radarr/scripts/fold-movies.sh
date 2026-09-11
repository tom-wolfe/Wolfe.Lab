#!/bin/bash
# One-off, at the desk, BEFORE Radarr's Library Import: Radarr requires a
# folder per movie and will not see a video file sitting loose in a root
# folder (wiki FAQ, "Can all my movie files be stored in one folder — No").
# This folds each loose video in <root> into <stem>/, and takes every
# same-stem sidecar with it — subtitles (<stem>.en.srt) and the artwork
# Jellyfin saved next to the file (<stem>-poster.jpg, <stem>-backdrop.jpg).
#
#   DRY=1 radarr/scripts/fold-movies.sh /Volumes/Data2/videos/movies   # plan
#         radarr/scripts/fold-movies.sh /Volumes/Data2/videos/movies   # do it
#
# Same-filesystem renames, so it is instant and touches no bytes of media.
# Every path changes, so Jellyfin will mint new item IDs for these movies
# — accepted for the whole Phase A pass (ROADMAP.md item 5); one full
# scan at the end of the pass, not one per step.
set -euo pipefail
shopt -s nullglob

root="${1:?usage: fold-movies.sh <root>}"
cd "$root"

for f in *.mkv *.mp4 *.m4v *.avi *.mov *.wmv; do
  case "$f" in
    ._*) continue ;;   # AppleDouble litter macOS writes on exFAT — not a movie
  esac
  stem="${f%.*}"
  if [ -e "$stem" ]; then
    echo "skip: '$stem/' already exists — fold by hand" >&2
    continue
  fi
  echo "$stem/"
  printf '    %s\n' "$stem".* "$stem"-*
  [ -n "${DRY:-}" ] && continue
  mkdir -- "$stem"
  mv -- "$stem".* "$stem"-* "$stem"/
done
