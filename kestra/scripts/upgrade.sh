#!/bin/bash
# Manual, guarded upgrade for the kestra stack — the ONE stack with no
# deploy flow, because Kestra can't safely replace its own executor
# mid-execution (see README.md).
#
# The workflow:
#   1. Bump the image pin(s) in compose.yaml via a normal PR. The chezmoi
#      tick ships the merge to this checkout, where it sits inert.
#   2. On the mini:  kestra/scripts/upgrade.sh [expected-version]
#
# The optional argument is an ASSERTION, not an instruction: this script
# only ever converges to what compose.yaml pins — the argument guards
# against running before the tick has landed your bump (or upgrading to
# something you never reviewed). Read the Kestra release notes before
# bumping (schema migrates forward only); Postgres majors additionally need
# pg_upgrade or dump/restore — the data dir is not forward-compatible.
set -euo pipefail

# Docker Desktop's CLI lives in /usr/local/bin, which a non-interactive SSH
# session doesn't have on PATH — path_helper only runs for login shells, so
# a bare `ssh macmini.local <this script>` gets /usr/bin:/bin:/usr/sbin:/sbin
# and dies on `docker: command not found` before it reaches the backup. Every
# flows/*/script.sh already does this; this one was missed because it is
# documented as an interactive command.
export PATH="$PATH:/usr/local/bin"

slice="$(cd "$(dirname "$0")/.." && pwd)"

pinned="$(sed -n 's|^ *image: kestra/kestra:||p' "$slice/compose.yaml" | tr -d ' ')"
if [ -z "$pinned" ]; then
  echo "upgrade: could not read the kestra image pin from $slice/compose.yaml" >&2
  exit 1
fi

if [ -n "${1:-}" ] && [ "$1" != "$pinned" ]; then
  echo "upgrade: compose.yaml pins '$pinned' but you expected '$1'." >&2
  echo "Has the tick shipped your bump yet? (chezmoi update, or git pull)" >&2
  exit 1
fi

# Backup before anything else — upgrades migrate the schema irreversibly.
# Skipped only if the db container isn't running (e.g. re-running after a
# failed upgrade already replaced it); a backup that *fails* still aborts
# the upgrade (set -e). The dump itself lives in flows/backup/script.sh so it can
# also run standalone or, later, on a schedule.
if [ "$(docker inspect -f '{{.State.Running}}' kestra-db 2>/dev/null)" = "true" ]; then
  "$slice/flows/backup/script.sh" "pre-$pinned"
else
  echo "WARNING: kestra-db is not running — continuing WITHOUT a backup" >&2
fi

echo "pulling images"
docker-compose --project-directory "$slice" pull
echo "converging to kestra/kestra:$pinned"
docker-compose --project-directory "$slice" up -d --remove-orphans

# Any HTTP status (401 included — the UI is behind basic auth) means the
# server is answering; only a dead TCP/HTTP stack reports 000.
printf 'waiting for kestra to answer on :8180 '
for _ in $(seq 1 60); do
  code="$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 http://localhost:8180/ || true)"
  if [ "$code" != "000" ]; then
    printf '\nkestra %s is up (HTTP %s).\n' "$pinned" "$code"
    exit 0
  fi
  printf '.'
  sleep 2
done
printf '\n'
echo "upgrade: kestra did not answer on :8180 within 120s — check 'docker logs kestra'." >&2
exit 1
