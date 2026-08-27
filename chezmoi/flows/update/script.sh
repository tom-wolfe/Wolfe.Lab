#!/bin/bash
# The CD game tick, host side: pull the repo and converge machine config.
# This script never deploys — the per-service deploy flows chain on the
# flow's SUCCESS in Kestra. Invoked on the mini as `chezmoi/update` by
# lab-job, which provides PATH and the headless DOCKER_CONFIG.
set -euo pipefail

# 1Password fallback for create_ templates that read the vault (e.g.
# Docker/caddy/caddy.env): headless sessions have no desktop-app
# integration, so hand op the machine's service account. Harmless when
# unused — create_ targets that already exist never evaluate their
# templates, so op is only invoked when one is missing.
token_file="$HOME/Docker/1password/service-account-token"
if [ -z "${OP_SERVICE_ACCOUNT_TOKEN:-}" ] && [ -s "$token_file" ]; then
  OP_SERVICE_ACCOUNT_TOKEN=$(cat "$token_file")
  export OP_SERVICE_ACCOUNT_TOKEN
fi

# --init: regenerate the config when .chezmoi.toml.tmpl changes — the
# config is derived state, and without this chezmoi warns and applies with
# stale data forever. Headless-safe because the template only uses
# promptChoiceOnce, which reuses the stored answers instead of prompting.
exec chezmoi update --init --no-tty
