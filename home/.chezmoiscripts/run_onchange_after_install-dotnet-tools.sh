#!/bin/bash

set -euo pipefail

if [ -x /opt/homebrew/bin/brew ]; then
  eval "$(/opt/homebrew/bin/brew shellenv)"
elif [ -x /usr/local/bin/brew ]; then
  eval "$(/usr/local/bin/brew shellenv)"
fi

command -v dotnet >/dev/null || { echo "dotnet not found - did brew bundle run?" >&2; exit 1; }

# Tool apphosts can't locate brew's keg runtime on their own
export DOTNET_ROOT="$(dirname "$(dirname "$(readlink -f "$(command -v dotnet)")")")/libexec"

# update -g installs if missing, updates if present
dotnet tool update -g centralisedpackageconverter
dotnet tool update -g nschema
dotnet tool update -g ritten

# Static zsh completion, regenerated whenever this script re-runs
"$HOME/.dotnet/tools/nschema" completion zsh > "$(brew --prefix)/share/zsh/site-functions/_nschema"
