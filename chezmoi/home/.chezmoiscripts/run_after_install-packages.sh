#!/bin/bash
# Install anything declared in ~/.Brewfile that isn't installed yet.
#
# Every machine, every apply: on a laptop the human running `chezmoi
# apply` is the actor; on a server it is the `chezmoi` workflow, which runs
# `chezmoi update` on the push that changes chezmoi/. A satisfied run costs
# about a second.
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

# Never refetch Homebrew's metadata here: the lab must keep working with
# the internet down, and nothing on a server refreshes it except the
# hand-run upgrade (chezmoi/README.md), which does so first.
export HOMEBREW_NO_AUTO_UPDATE=1

if [ -x /opt/homebrew/bin/brew ]; then
  eval "$(/opt/homebrew/bin/brew shellenv)"
elif [ -x /usr/local/bin/brew ]; then
  eval "$(/usr/local/bin/brew shellenv)"
fi

exec brew bundle install --file="$HOME/.Brewfile" --no-upgrade
