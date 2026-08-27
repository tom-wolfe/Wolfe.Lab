# Caddy with the Netlify DNS module compiled in. Stock Caddy ships no DNS
# providers, and the lab's certificates come from DNS-01 challenges against
# Netlify (see Caddyfile) — so this build is load-bearing, not cosmetic.
#
# Pinned like every other image. When bumping, keep BOTH stages on the same
# version — a builder/runtime mismatch produces a binary that silently
# reports the wrong version and skips upstream's compatibility checks.
FROM caddy:2.10-builder AS builder

RUN xcaddy build \
    --with github.com/caddy-dns/netlify

FROM caddy:2.10

COPY --from=builder /usr/bin/caddy /usr/bin/caddy
