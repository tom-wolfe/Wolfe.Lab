#!/bin/bash
# The lab's one door to the vault.
#
#   secrets.sh run [--env-file <file>]... -- <command> [arg...]
#       Run a command with the references in each env file resolved.
#   secrets.sh read <reference>
#       Print one secret, no trailing newline.
set -euo pipefail

token_file="$HOME/Docker/1password/service-account-token"
if [ -z "${OP_SERVICE_ACCOUNT_TOKEN:-}" ] && [ -s "$token_file" ]; then
  OP_SERVICE_ACCOUNT_TOKEN=$(cat "$token_file")
  export OP_SERVICE_ACCOUNT_TOKEN
fi

usage() {
  echo "usage: secrets.sh run [--env-file <file>]... -- <command...>" >&2
  echo "       secrets.sh read <reference>" >&2
  exit 64
}

case "${1:-}" in
  run)
    shift
    files=()
    while [ $# -gt 0 ]; do
      case "$1" in
        --env-file)   [ $# -ge 2 ] || usage; files+=(--env-file "$2"); shift 2 ;;
        --env-file=*) files+=(--env-file "${1#*=}"); shift ;;
        --)           shift; break ;;
        *)            usage ;;
      esac
    done
    [ $# -gt 0 ] || usage
    if [ "${#files[@]}" -eq 0 ]; then
      exec "$@"
    fi
    exec op run "${files[@]}" -- "$@"
    ;;
  read)
    [ $# -eq 2 ] || usage
    exec op read -n "$2"
    ;;
  *)
    usage
    ;;
esac
