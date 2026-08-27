# Caddy with the Netlify DNS module compiled in. Stock Caddy ships no DNS
# providers, and the lab's certificates come from DNS-01 challenges against
# Netlify (see Caddyfile) — so this build is load-bearing, not cosmetic.
#
# Pinned like every other image. When bumping, keep BOTH stages on the same
# version — a builder/runtime mismatch produces a binary that silently
# reports the wrong version and skips upstream's compatibility checks.
FROM caddy:2.10-builder AS builder

# PATCH (2026-08-27): libdns/netlify v1.2.0 types a zone's `domain` field
# as string (via Netlify's own stale open-api models) — but for domains
# REGISTERED THROUGH Netlify the live API returns an OBJECT there (the
# registration record: auto_renew, expires_at, ...), so zone parsing dies
# with `cannot unmarshal object into Go struct field netlifyZone.DNSZone.domain`
# before the ACME TXT record can even be presented. twolfe.dev is
# Netlify-registered, so this hits us and not most users. The client only
# ever reads a zone's id and name; replace the embedded model with exactly
# those. The grep makes the patch fail LOUDLY if upstream changes shape —
# when it 500s here, check whether upstream fixed it and drop all this.
RUN apk add --no-cache git \
    && git clone --depth 1 --branch v1.2.0 https://github.com/libdns/netlify /src/libdns-netlify \
    && sed -i 's|\*models\.DNSZone|ID string `json:"id"`; Name string `json:"name"`|' /src/libdns-netlify/models.go \
    && grep -q 'json:"id"' /src/libdns-netlify/models.go

RUN xcaddy build \
    --with github.com/caddy-dns/netlify \
    --with github.com/libdns/netlify=/src/libdns-netlify

FROM caddy:2.10

COPY --from=builder /usr/bin/caddy /usr/bin/caddy