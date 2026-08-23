{{ if eq .machine "server" -}}
#!/bin/bash
# Reload when agents change:
# {{ include "Library/LaunchAgents/md.obsidian.headless-sync-dnd.plist" | sha256sum }}
# {{ include "Library/LaunchAgents/md.obsidian.headless-sync-main.plist" | sha256sum }}
set -euo pipefail
uid=$(id -u)
for plist in "$HOME"/Library/LaunchAgents/*.plist; do
  launchctl bootout "gui/$uid" "$plist" 2>/dev/null || true   # unload if loaded
  launchctl bootstrap "gui/$uid" "$plist"                     # load fresh
done
{{ end -}}
