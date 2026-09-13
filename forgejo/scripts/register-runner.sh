#!/bin/bash
# Register a node's Actions runner with Forgejo:
#
#   forgejo/scripts/register-runner.sh <hostname> [scope]   # scope: owner or owner/repo, default tom-wolfe/Wolfe.Lab
#
# Run on the mini (needs the forgejo container) with op signed in. 
# Safe to re-run: registering an existing secret updates the runner in place.
set -euo pipefail

name="${1:?usage: register-runner.sh <hostname>}"
vault="Wolfe.Lab"
item="forgejo-runner-$name"
# The runner acts on the host, so only allow te lab to use it.
# Other projects use the containerized runner.
scope="${2:-tom-wolfe/Wolfe.Lab}"

# One secret per node, minted by Forgejo itself (40 hex chars) and kept in
# the vault as the origin. The node reads it once through a create_ template.
if ! op item get "$item" --vault "$vault" >/dev/null 2>&1; then
  secret="$(docker exec forgejo forgejo forgejo-cli actions generate-secret)"
  op item create --category "API Credential" --vault "$vault" \
    --title "$item" "credential=$secret" >/dev/null
  echo "created vault item $item"
fi

# The node's name is its label; `host` = steps run in a shell on the node.
op read "op://$vault/$item/credential" \
  | docker exec -i forgejo forgejo forgejo-cli actions register \
      --secret-stdin --name "$name" --labels "$name:host" --scope "$scope"
echo "registered runner $name (label $name:host, scope $scope)"
