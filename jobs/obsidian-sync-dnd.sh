#!/bin/bash
# One-shot sync of the Dungeons & Dragons Obsidian vault from Google Drive.
# Invoked on the mini by lab-job.
set -euo pipefail

exec "$HOME/.local/bin/nvm-run" ob sync \
  --path "$HOME/Library/CloudStorage/GoogleDrive-trwolfe13@gmail.com/My Drive/Obsidian/Dungeons & Dragons"
