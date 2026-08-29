#!/bin/bash
# Install anything declared in ~/.Brewfile that isn't installed yet, and
# nothing more. Invoked through the lab-job bridge as `chezmoi/packages` by
# ./flow.yaml beside it, which chains on every green chezmoi tick.
# Convergent and cheap: a satisfied run is about a second.
set -euo pipefail

# Homebrew isn't on a non-interactive SSH session's PATH (path_helper only
# runs for login shells), so resolve it the way the install scripts do.
if [ -x /opt/homebrew/bin/brew ]; then
  eval "$(/opt/homebrew/bin/brew shellenv)"
elif [ -x /usr/local/bin/brew ]; then
  eval "$(/usr/local/bin/brew shellenv)"
fi

# Two guards that keep this flow off the network, because it runs ~96 times
# a day on the tick and the lab is supposed to keep working with the
# internet down:
#   NO_AUTO_UPDATE  — don't refetch Homebrew's metadata every 15 minutes.
#                     lab.chezmoi/packages-upgrade does `brew update` once
#                     a night, which is the right cadence for it.
#   --no-upgrade    — `brew bundle` upgrades outdated formulae by DEFAULT.
#                     That default is exactly what coupled version churn to
#                     unrelated Brewfile edits; moving versions is the other
#                     flow's job.
export HOMEBREW_NO_AUTO_UPDATE=1

exec brew bundle install --file="$HOME/.Brewfile" --no-upgrade
