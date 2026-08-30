# Caddy

The lab's front door: one container terminating TLS on `:443` and routing
by hostname to containers on the shared `lab` Docker network. Exists so
addresses are names (`https://jellyfin.lab.twolfe.dev`), not ports
(`macmini.local:8096`).

This slice deliberately owns ONLY the shared edge concerns:

- the Caddy container — the STOCK image, a pure proxy: it terminates TLS
  and routes, and does nothing else;
- the `lab` Docker network every proxied service joins;
- the two wildcard DNS records (`tofu/`): `*.lab.twolfe.dev` → the
  mini's LAN address, `*.ts.twolfe.dev` → its Tailscale address. Same
  door, two ways in — `.lab` for anything in the house, `.ts` for
  tailnet devices anywhere (see tofu/records.tf for the decision). Both
  point at the front door itself, so the front door owns them;
- the wildcard certificate — ONE cert carrying both wildcard SANs —
  obtained and renewed OUTSIDE caddy by the `renew-certs` job
  (`flows/renew-certs/`): lego solves DNS-01 against Netlify nightly and
  reloads caddy when the cert changes.

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
   @myservice host myservice.lab.twolfe.dev myservice.ts.twolfe.dev
   handle @myservice {
       reverse_proxy myservice:1234
   }
   ```

   Both hostnames, always — `.lab` is the LAN path, `.ts` the tailnet
   path, and one handle serves both. A service whose app validates the
   Host header (qbittorrent) must whitelist both.

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
4. **First certificate**: run `flows/renew-certs/script.sh` on the mini
   (or `caddy/renew-certs` via the bridge). Caddy loads the cert from
   files and cannot START without them — setup.sh encodes this ordering.
5. **First deploy**: `caddy/deploy` via the job bridge (or
   `docker-compose up -d` in this directory on the mini). Must happen
   ONCE before redeploying any proxied slice — this compose creates the
   `lab` network the others reference as external.
6. **Re-up the proxied slices** (forgejo, kestra, jellyfin, garage) so
   their containers join the network. Compose recreates them — brief
   downtime each.

If LAN clients can't resolve `*.lab.twolfe.dev` while phones on mobile
data can: that's the router's DNS-rebind protection refusing public names
that resolve to RFC1918 space. Allowlist `twolfe.dev` in the router.

## Upgrading

Caddy: bump the `image:` pin in `compose.yaml`, redeploy. lego: bump the
image tag in `flows/renew-certs/script.sh`, and check the lego release
notes — a major bump can change the CLI (v4→v5 did).

## Operational notes

- Certificate, key and ACME account live in `~/Docker/caddy/lego` —
  state, backed up like all state. Losing it means re-issuing (Let's
  Encrypt rate limits apply), not disaster. `~/Docker/caddy/data` is
  caddy's own runtime state, modest now that ACME moved out.
- Renewal health is a Kestra concern: the nightly `renew-certs` run is
  a no-op until lego's ARI window opens, so a red run means the chain
  broke with weeks of certificate lifetime still banked.
- `docker exec caddy caddy validate --config /etc/caddy/Caddyfile` checks
  config (including all snippets) without touching the running instance.
- The repo mount is read-only and safe: the repo contains `op://`
  references, never secret material.
- Headless pulls of uncached images (a bumped caddy or lego pin, through
  the bridge) work because of the null credential helper the headless
  Docker config names — without it, macOS defaults to the osxkeychain
  helper and the locked login keychain kills the pull (CHANGELOG 0.9.1).
- Port 3000/8096/8180 publishes stay for now — automation (kestra flows,
  tofu providers, the job bridge docs) targets `macmini.local:<port>` and
  keeps working when the front door doesn't.
