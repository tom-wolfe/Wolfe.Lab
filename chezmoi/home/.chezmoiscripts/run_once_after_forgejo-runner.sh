#!/bin/bash
# First-time enable of the node's Actions runner and its change watcher.
set -euo pipefail

systemctl --user daemon-reload
systemctl --user enable --now forgejo-runner.path forgejo-runner.service
