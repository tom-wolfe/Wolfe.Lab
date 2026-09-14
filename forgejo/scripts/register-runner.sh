#!/bin/bash
# Register a node's Actions runner with Forgejo:
#
#   forgejo/scripts/register-runner.sh <hostname> [host|docker]   # default host
#
# Run on the mini (needs the forgejo container) with op signed in.
# Safe to re-run: registering an existing secret updates the runner in place.
set -euo pipefail

repo="$(cd "$(dirname "$0")/../.." && pwd)"

host="${1:?usage: register-runner.sh <hostname> [host|docker]}"
kind="${2:-host}"
vault="Wolfe.Lab"
case "$kind" in
  # Host runner: steps run in a shell on the node, so only the lab may use
  # it. The node's name is its label.
  host)   name="$host";        labels="$host:host";                 scope="tom-wolfe/Wolfe.Lab" ;;
  # Containerized runner: jobs in fresh containers, instance-wide, a ROLE
  # label shared by every node (a build lands wherever one is idle).
  docker) name="$host-docker"; labels="docker:docker://node:22-bookworm"; scope="" ;;
  *) echo "register-runner: kind must be host or docker" >&2; exit 64 ;;
esac
item="forgejo-runner-$name"

# One secret per node, minted by Forgejo itself (40 hex chars) and kept in
# the vault as the origin. The node reads it once through a create_ template.
if ! op item get "$item" --vault "$vault" >/dev/null 2>&1; then
  secret="$(docker exec -u git forgejo forgejo forgejo-cli actions generate-secret)"
  op item create --category "API Credential" --vault "$vault" \
    --title "$item" "credential=$secret" >/dev/null
  echo "created vault item $item"
fi

# -u git: Forgejo refuses to run as root, which is what a bare exec is.
# -n: no trailing newline — --secret-stdin counts it (41 != 40).
"$repo/scripts/secrets.sh" read "op://$vault/$item/credential" \
  | docker exec -i -u git forgejo forgejo forgejo-cli actions register \
      --secret-stdin --name "$name" --labels "$labels" ${scope:+--scope "$scope"}
echo "registered runner $name (label $labels, scope ${scope:-instance})"
