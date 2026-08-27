# Caddy

The lab's front door: one container terminating TLS on `:443` and routing
by hostname to containers on the shared `lab` Docker network. Exists so
addresses are names (`https://jellyfin.lab.twolfe.dev`), not ports
(`macmini.local:8096`).

This slice deliberately owns ONLY the shared edge concerns:

- the Caddy container and its build (stock Caddy has no DNS providers —
  the `Dockerfile` compiles in `caddy-dns/netlify` for DNS-01);
- the `lab` Docker network every proxied service joins;
- the `*.lab.twolfe.dev` wildcard DNS record (`tofu/`) — it points at the
  front door itself, so the front door owns it;
- the wildcard certificate.

Routes and public DNS names do NOT live here — see the contract below.
Netlify is a *provider*, not a slice: any slice needing a DNS record
configures it in its own tofu root.

## The contract: how a slice gets a hostname

Everything happens in the service's own slice; this one is never edited.

1. Join the `lab` network in the slice's compose file
   (`networks: [lab]`, declared `external: true` — multi-service stacks
   list `default` too, or they lose their internal network).
2. Drop a `<slice>/caddy.caddyfile` next to the compose file:

   ```
   @myservice host myservice.lab.twolfe.dev
   handle @myservice {
       reverse_proxy myservice:1234
   }
   ```

   The upstream is the CONTAINER name and port, not the host publish.
   The matcher/handle shape (rather than a site block) is because snippets
   are imported inside the wildcard site — one `*.lab` certificate instead
   of per-name certs, which would list every internal hostname in public
   Certificate Transparency logs.
3. Redeploy caddy (`caddy/deploy`, or wait for the tick) — the deploy
   script reloads config explicitly, because snippets arrive via the repo
   bind mount and don't change compose's config hash.
4. Add the row in `ENDPOINTS.md`, same commit.

For deliberate PUBLIC exposure (`jellyfin.twolfe.dev`), the service's own
tofu root declares the record with the netlify provider — pattern in
`tofu/records.tf` here, pointed at the Tailscale IP when that lands.

## Names are for humans

Service-to-service traffic on the mini uses the Docker network directly
(`http://kestra:8080`), never the public names. The lab must keep working
with the internet down; DNS for `*.lab.twolfe.dev` lives on Netlify's
nameservers and resolves only while the internet is up. The names are
sugar for browsers, not plumbing.

## Bootstrap (one-time, in order)

1. **1Password items** (Wolfe.Lab vault): `netlify-pat` (exists) and
   `tofu-state-passphrase` — create as a Password item, e.g.
   `op item create --vault Wolfe.Lab --category password --title tofu-state-passphrase --generate-password=64,letters,digits`.
   If `op run` later errors on a field name, the reference in
   `tofu/secrets.env` doesn't match the item's category — see the comment
   there.
2. **DNS record**: `cd tofu && tofu init && op run --env-file=secrets.env -- tofu apply`.
   Check `lab_ipv4` still matches the mini first, and give the mini a DHCP
   reservation if it doesn't have one — the record is only as stable as
   the address.
3. **Secrets on the mini**: `chezmoi apply` materializes
   `~/Docker/caddy/caddy.env` — a `create_` template (server machines
   only, see `.chezmoiignore`): chezmoi evaluates it ONLY while the file
   is missing, so `op` and the internet are bootstrap dependencies, not
   tick dependencies. Rotation: delete the file, `chezmoi apply` again
   (GUI session, or any session on the mini — the update flow exports the
   1P service account for headless runs).
4. **First deploy**: `caddy/deploy` via the job bridge (or
   `docker-compose up -d --build` in this directory on the mini). Must
   happen ONCE before redeploying any proxied slice — this compose
   creates the `lab` network the others reference as external.
5. **Re-up the proxied slices** (forgejo, kestra, jellyfin, garage) so
   their containers join the network. Compose recreates them — brief
   downtime each.

If LAN clients can't resolve `*.lab.twolfe.dev` while phones on mobile
data can: that's the router's DNS-rebind protection refusing public names
that resolve to RFC1918 space. Allowlist `twolfe.dev` in the router.

## Upgrading

Bump BOTH `FROM` lines in `Dockerfile` (same version!) and the `image:`
tag in `compose.yaml`, then `docker compose up -d --build`. Check the
Caddy release notes; the netlify module is rebuilt from its default branch
on every image build.

## Operational notes

- Certificates and the ACME account key live in `~/Docker/caddy/data` —
  state, backed up like all state. Losing it means re-issuing certs
  (Let's Encrypt rate limits apply), not disaster.
- `docker exec caddy caddy validate --config /etc/caddy/Caddyfile` checks
  config (including all snippets) without touching the running instance.
- The repo mount is read-only and safe: the repo contains `op://`
  references, never secret material.
- The deploy's `--build` works headless only because of two chezmoi-managed
  pieces: buildx symlinked into `~/.docker-headless/cli-plugins/` (compose
  needs it or falls back to the legacy builder), and the null credential
  helper `docker-credential-headless` that the headless config names —
  without it, macOS defaults to the osxkeychain helper and any uncached
  base-image pull dies on the locked login keychain (CHANGELOG 0.9.1).
- Port 3000/8096/8180 publishes stay for now — automation (kestra flows,
  tofu providers, the job bridge docs) targets `macmini.local:<port>` and
  keeps working when the front door doesn't.
