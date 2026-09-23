# Caddy runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap (one-time, in order)

1. **1Password items** (Wolfe.Lab vault): `netlify-pat` (exists) and
   `tofu-state-passphrase` — create as a Password item, e.g.
   `op item create --vault Wolfe.Lab --category password --title tofu-state-passphrase --generate-password=64,letters,digits`.
2. **DNS records**: `lab deploy` from `caddy/tofu` (the `tofu caddy` workflow,
   or by hand: `cd caddy/tofu && dotnet run --project ../../build/src/Wolfe.Lab.Build -- deploy`).
   Check `lab_tailscale_ipv4` still matches the mini first — the record
   is only as stable as the address, and a re-enrolment that mints a new
   one means updating the variable and re-applying.
3. **First certificate**: `lab renew` from `caddy/certs/` on the mini (or
   the `caddy certs` workflow); it reads the Netlify token its
   `ritten.json` names, for the DNS-01 challenge.
   Caddy loads the cert from files and cannot START without them —
   setup.sh encodes this ordering.
4. **First deploy**: run the `caddy compose` workflow, then `caddy routes` (or
   `docker compose up -d` in this directory on the mini). Must happen
   ONCE before redeploying any proxied slice — this compose creates the
   `lab` network the others reference as external.
5. **Re-up the proxied slices** (forgejo, jellyfin, garage) so
   their containers join the network. Compose recreates them — brief
   downtime each.

If clients can't resolve `*.twolfe.dev` while another network can:
that's the router's DNS-rebind protection refusing public names that
resolve to private space. It was RFC1918 that tripped it when the LAN
wildcard existed; the names resolve to 100.x now, which some routers
also treat as private. Allowlist `twolfe.dev` in the router.

## Cutting over to one name per service

A flag day, and one that has to happen AT THE DESK: both the certificate
and the tofu apply resolve secrets through `op`, which does not answer
when you are away from it.

Two facts set the order, and neither is recoverable by re-running a
workflow:

- **The certificate filename follows the first domain.** `*.twolfe.dev`
  issues to `_.twolfe.dev.*`, the Caddyfile's `tls` paths changed to
  match, and caddy cannot start until a certificate exists at the path
  it names. Merging before the cert exists takes the front door down.
- **The zone's negative TTL is an hour.** A resolver that looks up a new
  name before the record exists caches the NXDOMAIN for 3600s. Gatus
  deploys on the Pi, in a different concurrency group from everything on
  the mini, so on a merge it races — and if it asks first, the board
  stays red for an hour over a name that is actually fine. So DNS goes
  in BEFORE the merge, not as part of it.

1. **Certificate.** Dispatch the `caddy certs` workflow **on this
   branch** — there is no clone of the repo on the node to run it from by
   hand. The runner checks the branch out on the mini, where `op` can
   answer.

   Nothing needs moving out of the way first. The usual ritual — shifting
   the old files aside so lego reissues — exists for a changed SAN list
   under an UNCHANGED first domain, where the filename would collide and
   lego would see a valid cert and no-op. Here the first domain itself
   changes, so the new certificate lands beside the old one under a new
   name and the old one stays put as the rollback.

   Confirm on the mini afterwards:

       ssh macmini.local
       openssl x509 -in ~/Docker/caddy/lego/certificates/_.twolfe.dev.crt -noout -text | grep DNS

   Nothing is serving it yet: the running caddy still names the old file.

2. **DNS**, by hand from the branch, on the LAN — the state backend is
   `http://macmini.local:3900`, so this only works where mDNS resolves
   the mini. One root at a time; `deploy` plans first and stops at the
   gate with the plan on screen, so read it there before approving:

       cd caddy/tofu && dotnet run --project ../../build/src/Wolfe.Lab.Build -- deploy
       cd ../../forgejo/tofu && dotnet run --project ../../build/src/Wolfe.Lab.Build -- deploy
       cd ../../gatus/tofu && dotnet run --project ../../build/src/Wolfe.Lab.Build -- deploy

   What the plans should say. For `caddy`: two destroys and two creates.
   Those old wildcards carry `prevent_destroy`, which only protects a
   resource still present in the configuration — both blocks are gone
   from `records.tf` now, so the destroy is allowed.

   **For `forgejo` and `gatus`, look specifically for "forces
   replacement" on the `code` and `status` records.** Those two blocks
   DO still exist and DO still carry `prevent_destroy`, and their CNAME
   value changed. If the provider treats the value as replaceable
   in-place, the plan is a clean update and there is nothing to do. If it
   forces replacement, the apply will refuse with a `prevent_destroy`
   error — comment the `lifecycle` block out of that one record, apply,
   then put it back in the same commit.

   By hand on purpose. `caddy-tofu.yaml` applies on push WITHOUT a plan
   review, so merging would do all of this unattended — and this is the
   one apply in the repo that destroys something.

   The old names stop being served here, so the front door is unreachable
   by name until step 3. Go straight on.

3. **Merge.** caddy redeploys onto the new Caddyfile and the certificate
   from step 1; chezmoi installs ollama on the mini; the ollama slice
   converges; Gatus redeploys on the Pi against names that now resolve.

   The mini's workflows share one concurrency group and run in an
   arbitrary order, so `ollama` may run before `chezmoi` has installed
   the binary. It fails clearly — "not on this node" — and a re-run fixes
   it. Nothing else in the merge is order-sensitive once step 1 is done.

4. **Pull a model**, before Gatus's `ai` check trips — three failures at
   a 2-minute interval is about six minutes. See `ollama/RUNBOOK.md`.

5. **Check.** Every name in `ENDPOINTS.md`, and the `lab` group in Gatus
   going green.

If step 3 lands before step 1, caddy will not start, because the
certificate file it names does not exist. The fix is step 1 followed by a
re-run of the `caddy routes` workflow.

## Upgrading

Caddy: bump the `image:` pin in `compose/compose.yaml`, redeploy. lego: bump
`image` in `certs/ritten.json`, and check the lego release
notes — a major bump can change the CLI (v4→v5 did).
