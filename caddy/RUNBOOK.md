# Caddy runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

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
   `~/Docker/caddy/caddy.env` — a `create_` template (the `macmini-node`
   profile only): chezmoi evaluates it ONLY while the file
   is missing, so `op` and the internet are bootstrap dependencies, not
   tick dependencies. Rotation: delete the file, `chezmoi apply` again
   (GUI session, or any session on the mini — the update flow exports the
   1P service account for headless runs).
4. **First certificate**: run `flows/renew-certs/script.sh` on the mini
   (or the renew-certs workflow). Caddy loads the cert from
   files and cannot START without them — setup.sh encodes this ordering.
5. **First deploy**: run the caddy workflow (or
   `docker compose up -d` in this directory on the mini). Must happen
   ONCE before redeploying any proxied slice — this compose creates the
   `lab` network the others reference as external.
6. **Re-up the proxied slices** (forgejo, jellyfin, garage) so
   their containers join the network. Compose recreates them — brief
   downtime each.

If LAN clients can't resolve `*.lab.twolfe.dev` while phones on mobile
data can: that's the router's DNS-rebind protection refusing public names
that resolve to RFC1918 space. Allowlist `twolfe.dev` in the router.

## Upgrading

Caddy: bump the `image:` pin in `compose.yaml`, redeploy. lego: bump the
image tag in `flows/renew-certs/script.sh`, and check the lego release
notes — a major bump can change the CLI (v4→v5 did).
