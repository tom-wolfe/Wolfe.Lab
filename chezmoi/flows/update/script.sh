#!/bin/bash
# The CD game tick, host side: pull the repo and converge machine config.
# This script never deploys — the per-service deploy flows chain on the
# flow's SUCCESS in Kestra. Invoked on the mini as `chezmoi/update` by
# lab-job, which provides PATH and the headless DOCKER_CONFIG.
set -euo pipefail

exec chezmoi update --no-tty
