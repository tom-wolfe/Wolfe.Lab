#!/bin/bash
# The CD game tick, host side: pull the repo and converge machine config.
# This script never deploys — the per-service deploy flows chain on the
# flow's SUCCESS in Kestra. Invoked on the mini as `chezmoi/update` by
# lab-job, which provides PATH and the headless DOCKER_CONFIG.
set -euo pipefail

# --init: regenerate the config when .chezmoi.toml.tmpl changes — the
# config is derived state, and without this chezmoi warns and applies with
# stale data forever. Headless-safe because the template only uses
# promptChoiceOnce, which reuses the stored answers instead of prompting.
exec chezmoi update --init --no-tty
