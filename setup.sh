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
#   3. Docker Desktop installed and running. NOT from the Brewfile — the
#      cask sits in the non-server branch, so install it by hand here
#      (`brew install --cask docker-desktop`; it requires macOS >= 14).
# Then, from this repo's checkout:  ./setup.sh
#
# AFTER forgejo is up, repoint the chezmoi checkout at the primary — the
# bootstrap above necessarily clones the GitHub mirror, but CD should not
# depend on it (and the poke fires against the primary's push, so pulling
# the mirror races it):
#   git -C ~/.local/share/chezmoi remote set-url origin http://localhost:3000/tom-wolfe/Wolfe.Lab.git
# Anonymous loopback HTTP: the repo is public (forgejo/tofu/primary.tf),
# and the tick only ever pulls.
set -euo pipefail

repo="$(cd "$(dirname "$0")" && pwd)"

converge() {
  echo "==> $1"
  docker compose --project-directory "$repo/$1" up -d --remove-orphans "${@:2}"
}

# Caddy first — its compose OWNS the shared `lab` network that every other
# stack references as `external`, so nothing else can even `up` until this
# has created it. The certificate must exist BEFORE caddy starts (the
# Caddyfile loads it from files), hence renew-certs first — convergent,
# a no-op when the cert is already current.
"$repo/caddy/flows/renew-certs/script.sh"
converge caddy

# Garage next — it hosts the tofu state everything else's IaC backends onto.
converge garage
"$repo/garage/scripts/init-layout.sh"

converge forgejo
converge jellyfin

# Beszel's hub. Its AGENT is not started here — that's a Homebrew formula
# from the Brewfile, and it can't enrol until the hub has minted a token
# for it anyway. See beszel/README.md "Bootstrap" for that hand-off.
converge beszel

# Kestra last: once it's up and the flows are registered, it takes over.
converge kestra

cat <<'EOF'

Stacks are up. Remaining one-time steps:

  1. Register the flows (from any machine with op and the repo):
       cd kestra/tofu
       op run --env-file=secrets.env -- tofu init
       op run --env-file=secrets.env -- tofu apply

  2. Log in at http://macmini.local:8180 (1P: kestra-admin) and watch the
     first lab.chezmoi/update tick go green — the deploy flows chain from it,
     and from then on the lab converges itself.

  3. Enrol the monitoring agent — the one bootstrap that can't be ordered
     ahead of time, because the hub mints the token the agent needs:
       http://macmini.local:8090 -> create the superuser
       Settings -> Tokens -> copy the universal token and public key into
         a 1Password item `beszel-agent` (credential / username)
       chezmoi apply && brew services list
     Then set thresholds and the Pushover URL in the hub — it ships none,
     so nothing alerts until you do. Full runbook: beszel/README.md.
EOF
