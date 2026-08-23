#!/bin/bash

set -euo pipefail

if [ -x /opt/homebrew/bin/brew ]; then
  eval "$(/opt/homebrew/bin/brew shellenv)"
elif [ -x /usr/local/bin/brew ]; then
  eval "$(/usr/local/bin/brew shellenv)"
fi

# The dotnet-sdk cask installs to /usr/local/bin, which non-interactive SSH
# sessions don't have on PATH (path_helper only runs for login shells).
export PATH="$PATH:/usr/local/bin"

# update -g installs if missing, updates if present
dotnet tool update -g centralisedpackageconverter
dotnet tool update -g nschema
dotnet tool update -g ritten

# Static zsh completion, regenerated whenever this script re-runs.
# nschema emits a sourcing-style script; compinit needs a #compdef first line.
{
  echo "#compdef nschema"
  "$HOME/.dotnet/tools/nschema" completion zsh
  echo '_nschema_complete "$@"'
} > "$(brew --prefix)/share/zsh/site-functions/_nschema"
rm -f "$HOME"/.zcompdump*  # compinit caches bindings; force a rebuild
