#!/bin/bash
# One-shot sync of the Main Obsidian vault from Google Drive.
# Run every 10 minutes on the mini by .forgejo/workflows/obsidian.yaml.
set -euo pipefail

exec "$HOME/.local/bin/nvm-run" ob sync \
  --path "$HOME/Library/CloudStorage/GoogleDrive-trwolfe13@gmail.com/My Drive/Obsidian/Main"
