#!/bin/bash
# Deploy this slice's compose stack
set -euo pipefail

exec docker-compose \
  --project-directory "$(cd "$(dirname "$0")/../.." && pwd)" \
  up -d --remove-orphans
