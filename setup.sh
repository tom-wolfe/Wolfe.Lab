#!/bin/bash
# Fresh-server bring-up — the one imperative sequence that can't converge by
# itself, because Kestra (the thing that runs deploys) can't deploy itself
# into existence. Everything here is idempotent; re-runs are safe no-ops.
# Day-to-day this script is never needed: the chezmoi tick and the per-slice
# deploy flows own convergence (see README.md "How deployment works").
#
# Before running (see README.md "New machine bootstrap" for the details):
#   1. 1Password signed in, machine type "server" chosen during:
#   2. sh -c "$(curl -fsLS get.chezmoi.io)" -- init --ssh --apply tom-wolfe/Wolfe.Lab
#      — run `chezmoi apply` a second time: authorized_keys can only template
#      the job-bridge public key after the first apply has materialized it.
#   3. Docker Desktop installed (Brewfile) and running.
# Then, from this repo's checkout:  ./setup.sh
set -euo pipefail

repo="$(cd "$(dirname "$0")" && pwd)"

converge() {
  echo "==> $1"
  docker-compose --project-directory "$repo/$1" up -d --remove-orphans
}

# Garage first — it hosts the tofu state everything else's IaC backends onto.
converge garage
"$repo/garage/scripts/init-layout.sh"

converge forgejo
converge jellyfin

# Kestra last: once it's up and the flows are registered, it takes over.
converge kestra

cat <<'EOF'

Stacks are up. Remaining one-time steps:

  1. Register the flows (from any machine with op and the repo):
       cd kestra/tofu
       op run --env-file=secrets.env -- tofu init
       op run --env-file=secrets.env -- tofu apply

  2. Log in at http://macmini.local:8180 (1P: Kestra Admin) and watch the
     first chezmoi-update tick go green — the deploy flows chain from it,
     and from then on the lab converges itself.
EOF
