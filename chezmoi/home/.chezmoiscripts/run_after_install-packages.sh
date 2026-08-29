#!/bin/bash
# Install anything declared in ~/.Brewfile that isn't installed yet.
#
# This script runs on the LAPTOPS ONLY — the server ignores it
# (.chezmoiignore), because there the job belongs to lab.chezmoi/packages.
# chezmoi declares, Kestra acts; on a machine with no Kestra, the human
# running `chezmoi apply` is the actor, and since that is a manual and
# occasional act a plain `run_` (every apply) costs about a second.
#
# `run_after_` matters: this reads ~/.Brewfile, which chezmoi writes during
# the file pass. A `before` or unprefixed script could run first and act on
# a stale file, or none at all on a fresh machine.
#
# --no-upgrade is the point of the whole split. `brew bundle` upgrades
# outdated formulae by DEFAULT, which is what used to couple every version
# bump to whatever unrelated Brewfile edit happened to trigger the old
# run_onchange script. Installing what's missing and moving versions forward
# are different jobs with different reasons to happen.
set -euo pipefail

if [ -x /opt/homebrew/bin/brew ]; then
  eval "$(/opt/homebrew/bin/brew shellenv)"
elif [ -x /usr/local/bin/brew ]; then
  eval "$(/usr/local/bin/brew shellenv)"
fi

exec brew bundle install --file="$HOME/.Brewfile" --no-upgrade
