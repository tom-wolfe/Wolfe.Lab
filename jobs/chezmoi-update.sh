#!/bin/bash
# Converge this machine: pull the repo, apply chezmoi, redeploy changed
# compose stacks (via the deploy hook). Invoked on the mini by lab-job,
# which provides PATH and the headless DOCKER_CONFIG.
set -euo pipefail

exec chezmoi update --no-tty
