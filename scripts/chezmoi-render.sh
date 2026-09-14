#!/bin/bash
# Render the chezmoi source for one profile, on any machine, without
# applying anything: the files a machine of that profile would get, with
# their exact contents.
#
#   chezmoi-render.sh <profile> [out-dir]
#
# A scratch config supplies the profile, and every onepasswordRead renders
# as its own op:// reference (chezmoi's `onepassword.command` is pointed at
# a stub), so the vault is never touched. Externals are skipped — they are
# downloads, not templates. Scripts render like files, under
# .chezmoiscripts/. A file gated to other profiles is simply absent.
#
# CI runs this for every profile on each pull request (ci.yaml), so a
# template that only breaks on one machine fails there instead of on the
# merge. .chezmoi.homeDir is this machine's, so home-rooted paths read as
# the renderer's home — the shape is what to review, not the prefix.
set -euo pipefail

repo="$(cd "$(dirname "$0")/.." && pwd)"
profile="${1:?usage: chezmoi-render.sh <profile> [out-dir]}"
out="${2:-$(mktemp -d)}"

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

# Prints its last argument: the reference `op read --no-newline <ref>` got.
# shellcheck disable=SC2016
printf '#!/bin/sh\nfor a; do ref=$a; done\nprintf "%%s" "$ref"\n' > "$tmp/op"
chmod +x "$tmp/op"
cat > "$tmp/chezmoi.toml" <<CFG
[data]
  profile = "$profile"
[onepassword]
  command = "$tmp/op"
  mode = "service"
CFG
export OP_SERVICE_ACCOUNT_TOKEN=render

mkdir -p "$out"
chezmoi --source "$repo" --destination "$out" --config "$tmp/chezmoi.toml" \
  --cache "$tmp/cache" archive --exclude externals --output "$tmp/render.tar"
tar -x -C "$out" -f "$tmp/render.tar"

echo "$profile -> $out"
(cd "$out" && find . -type f | sed 's|^\./||' | sort)
