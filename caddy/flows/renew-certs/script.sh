#!/bin/bash
# Issue-or-renew the lab's wildcard certificate. Invoked nightly through
# the lab-job bridge as `caddy/renew-certs` by ./flow.yaml beside it, and
# once by setup.sh BEFORE caddy first starts — the Caddyfile loads the
# cert from FILES, so no cert means caddy cannot start.
#
# lego v5's `run` is convergent: registers + issues when nothing exists,
# renews when Let's Encrypt's ARI window says it's due, no-op otherwise —
# safe on any schedule. SSL maintenance lives HERE, not in caddy: the
# caddy-dns/netlify module is dead upstream (see CHANGELOG 0.9.1/0.9.2),
# lego's in-tree netlify provider is maintained, and a renewal that fails
# shows up as a red run in Kestra instead of a log line nobody reads.
set -euo pipefail

lego_dir="$HOME/Docker/caddy/lego"
mkdir -p "$lego_dir"

# NETLIFY_TOKEN — chezmoi-materialized cache, origin 1Password. Only this
# job sees it; the caddy container no longer carries any secret.
env_file="$HOME/Docker/caddy/caddy.env"
if [ ! -s "$env_file" ]; then
  echo "renew-certs: $env_file missing — run chezmoi apply first" >&2
  exit 1
fi

# Pinned like every image in the lab. To upgrade: bump the tag, re-run.
#
# Two hard-won flags (2026-08-28):
#  * State mounts at /state, NOT /lego — the v5 image ships its BINARY at
#    /lego, and mounting a directory over it fails with a misleading
#    "not a directory" error.
#  * --dns.propagation.wait REPLACES lego's DNS verification, because
#    in-container DNS verification is meaningless under Docker Desktop:
#    the VM intercepts ALL port-53 traffic, so lego's "direct" queries to
#    the authoritative servers actually hit Docker's DNS proxy — which
#    serves stale cached TXTs and sporadically answers REFUSED. Let's
#    Encrypt validates from its own resolvers on the real internet, where
#    Netlify's NS1 fleet propagates in seconds (measured <20s); 90s is
#    comfortable margin, deterministic, and immune to the proxy.
#
# Changing the DOMAIN LIST below does not reissue by itself: lego keys
# its stored state by the FIRST domain's filename and its convergence
# logic is about expiry, not SANs — do not assume an edited list forces
# a new certificate. The deterministic path: move the
# _.lab.twolfe.dev.* files out of ~/Docker/caddy/lego/certificates and
# re-run this job. Fresh issuance, same filenames (first domain), so the
# Caddyfile's tls paths never change. Verify with:
#   openssl x509 -in ~/Docker/caddy/lego/certificates/_.lab.twolfe.dev.crt -noout -text | grep DNS
docker run --rm \
  --env-file "$env_file" \
  -v "$lego_dir:/state" \
  goacme/lego:v5.4.0 \
  --log.format text \
  run \
  --accept-tos \
  --email trwolfe13@gmail.com \
  --dns netlify \
  --domains '*.lab.twolfe.dev' \
  --domains '*.ts.twolfe.dev' \
  --domains 'code.twolfe.dev' \
  --domains 'status.twolfe.dev' \
  --path /state \
  --dns.propagation.wait 90s

# Caddy holds certificates in memory; hand it the fresh files. Only when
# it's actually running — setup.sh calls this before caddy's first start.
# A running-but-unreloadable caddy IS a failure (the in-memory cert would
# eventually expire), so no `|| true` here.
#
# --force is LOAD-BEARING. A plain `caddy reload` compares the adapted
# config with the running one and, if identical, does nothing at all —
# and the certificate files are only re-read as part of a real load. A
# renewal changes the FILES, never the Caddyfile, so without --force every
# reload here was a silent no-op and caddy would have served the old cert
# until something else changed the config (found 2026-09-02, when a
# re-issued cert with new SANs sat on disk while caddy kept serving the
# previous one through two "successful" reloads).
if [ -n "$(docker ps -q -f name='^caddy$')" ]; then
  docker exec caddy caddy reload --config /etc/caddy/lab/caddy/Caddyfile --force
fi
