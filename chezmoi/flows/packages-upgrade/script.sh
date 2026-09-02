#!/bin/bash
# Nightly Homebrew upgrade. Invoked through the lab-job bridge as
# `chezmoi/packages-upgrade`, or by hand.
set -euo pipefail

if [ -x /opt/homebrew/bin/brew ]; then
  eval "$(/opt/homebrew/bin/brew shellenv)"
elif [ -x /usr/local/bin/brew ]; then
  eval "$(/usr/local/bin/brew shellenv)"
fi

# Refresh formula metadata. The tick-chained lab.chezmoi/packages flow runs
# with NO_AUTO_UPDATE precisely so this is the one place that reaches out.
brew update

# Declared packages FIRST, and through `brew bundle` rather than plain
# `brew upgrade`, because only bundle honours `restart_service: :changed`.
# `brew upgrade` replaces a binary and leaves the old process running --
# which for the beszel agent would mean a silently stale monitor. Going
# through bundle restarts what it upgraded.
brew bundle install --file="$HOME/.Brewfile" --upgrade

# Only update formulae, because casks includes rebooting Docker.
brew upgrade --formula

# No explicit cleanup: Homebrew runs `brew cleanup` itself after an upgrade
# unless HOMEBREW_NO_INSTALL_CLEANUP is set, which it isn't here.
brew services list
