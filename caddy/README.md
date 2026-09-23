# Caddy

The lab's front door: one container terminating TLS on `:443` and routing
by hostname to containers on the shared `lab` Docker network. Exists so
addresses are names (`https://jellyfin.twolfe.dev`), not ports
(`macmini.local:8096`).

This slice deliberately owns ONLY the shared edge concerns:

- the Caddy container — the STOCK image, a pure proxy: it terminates TLS
  and routes, and does nothing else;
- the `lab` Docker network every proxied service joins;
- the wildcard DNS record (`tofu/`): `*.twolfe.dev` → the mini's
  Tailscale address, plus `lab.twolfe.dev` for the door itself, which is what a slice CNAMEs to when it wants a name of its own.
- the wildcard certificate — ONE cert for `*.twolfe.dev` —
  obtained and renewed OUTSIDE caddy by the `certs/` component
  (`lab renew`, declared in its `ritten.json`): lego solves DNS-01
  against Netlify nightly and reloads caddy when the cert changes;
- the routes — every component's `caddy.caddyfile`, gathered by the
  `routes/` component into a release of its own that the Caddyfile
  imports, so the stack in `compose/` knows nothing of them.

Routes and public DNS names do NOT live here — see the contract below.
Netlify is a *provider*, not a slice: any slice needing a DNS record
configures it in its own tofu root. Records that belong to the domain
itself rather than to any slice (mail, verification) live in `dns/` —
its README records the boundary.

## The contract: how a service gets a hostname

Everything happens in the service's own slice; this one is never edited.

1. Join the `lab` network in the component's compose file
   (`networks: [lab]`, declared `external: true` — multi-service stacks
   list `default` too, or they lose their internal network).
2. Drop a `caddy.caddyfile` next to the compose file, in the same
   component (`<slice>/compose/caddy.caddyfile`):

   ```
   @myservice host myservice.twolfe.dev
   handle @myservice {
       reverse_proxy myservice:1234
   }
   ```

   ONE hostname. The component picks it and nothing else has to agree: the
   wildcard record and the wildcard certificate already cover whatever
   it chose, so there is no DNS to write, no SAN to add and no edit to
   this slice. Pick the name a person would say — `code`, not
   `forgejo` — because it is the only one there will be. A service whose
   app validates the Host header (qbittorrent) must be told it.

   The upstream is the CONTAINER name and port, not the host publish.
   The matcher/handle shape (rather than a site block) is because snippets
   are imported inside the wildcard site — one `*.lab` certificate instead
   of per-name certs, which would list every internal hostname in public
   Certificate Transparency logs.
3. Nothing: the `caddy routes` workflow fires on any `caddy.caddyfile`
   anywhere in the repository, gathers every one into the routes release
   and reloads the door explicitly — snippets arrive via a bind mount and
   never change compose's config hash. Run it from the Actions tab to
   force the gather.
4. Add the row in `ENDPOINTS.md`, same commit.

## The one name that is not a route

`git.twolfe.dev` is an A record at the forgejo sidecar's own tailnet IP,
not a route through this door, because SSH needs a machine where port 22
is free. It lives in `forgejo/tofu` and has nothing to do with this
slice.

Everything else is just a name under the wildcard. There used to be a
second shape here — a "neat name" at the apex, which matched no wildcard
and so needed a CNAME, a line in this slice's `Caddyfile`, its own
`--domains` SAN and a forced re-issue. That whole procedure is gone: the
wildcard moved up a label and swallowed it.

One consequence worth keeping: with a single `*.twolfe.dev` certificate,
NO service name appears in public Certificate Transparency logs — not
even the human ones, which used to be listed individually. The lab's
shape is no longer readable from the outside.

## Names are for humans

Service-to-service traffic on the mini uses the Docker network directly
(`http://forgejo:3000`), never the public names. The lab must keep working
with the internet down; DNS for `*.twolfe.dev` lives on Netlify's
nameservers and resolves only while the internet is up. The names are
sugar for browsers, not plumbing.

## Operational notes

- Certificate, key and ACME account live in `~/Docker/caddy/lego` —
  state, backed up like all state. Losing it means re-issuing (Let's
  Encrypt rate limits apply), not disaster. `~/Docker/caddy/data` is
  caddy's own runtime state, modest now that ACME moved out.
- Renewal health is the `caddy certs` workflow's concern: the nightly run is
  a no-op until lego's ARI window opens, so a red run means the chain
  broke with weeks of certificate lifetime still banked.
- **After any certificate change, the reload must be `--force`.** A
  plain `caddy reload` with an unchanged Caddyfile is a no-op and does
  not re-read the certificate files. The renewal job does this;
  if you ever re-issue by hand and reload yourself, so must you. The
  proof is what caddy serves, not what's on disk:
  `echo | openssl s_client -connect macmini.local:443 -servername code.twolfe.dev 2>/dev/null | openssl x509 -noout -text | grep DNS:`
- **Caddy never obtains certificates itself** — `auto_https disable_certs`
  in the Caddyfile. Before that was set, a site-address name the loaded
  cert didn't cover made caddy start its own ACME orders for it (it
  happened: the container was recreated a minute before a re-issued cert
  landed). Now such a name just fails its handshake until the renewal
  runs and force-reloads.
- `docker exec caddy caddy validate --config /etc/caddy/lab/caddy/Caddyfile`
  checks config (including all snippets) without touching the running
  instance. That path — the Caddyfile through the repo mount, not a
  file bind at `/etc/caddy/Caddyfile` — is deliberate: a single-file bind
  follows an inode, git replaces files by rename, and the first edit
  after container creation left caddy holding a deleted file
  (compose.yaml has the story). The same trap applies to any single
  file bound into any container.
- The repo mount is read-only and safe: the repo contains `op://`
  references, never secret material.
- Headless pulls of uncached images (a bumped caddy or lego pin, through
  the runner) work because of the null credential helper the headless
  Docker config names — without it, macOS defaults to the osxkeychain
  helper and the locked login keychain kills the pull (CHANGELOG 0.9.1).
- Port 3000/8096 publishes stay for now — automation (tofu providers, the
  Gatus lab checks) targets the mini by address and port and
  keeps working when the front door doesn't.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
