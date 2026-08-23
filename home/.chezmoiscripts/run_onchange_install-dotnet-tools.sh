#!/bin/bash

set -euo pipefail

if [ -x /opt/homebrew/bin/brew ]; then
  eval "$(/opt/homebrew/bin/brew shellenv)"
elif [ -x /usr/local/bin/brew ]; then
  eval "$(/usr/local/bin/brew shellenv)"
fi

# update -g installs if missing, updates if present
dotnet tool update -g centralisedpackageconverter
dotnet tool update -g nschema
dotnet tool update -g ritten

# Static zsh completion, regenerated whenever this script re-runs
"$HOME/.dotnet/tools/nschema" completion zsh > "$(brew --prefix)/share/zsh/site-functions/_nschema"
