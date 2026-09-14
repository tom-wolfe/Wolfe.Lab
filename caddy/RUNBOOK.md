# Caddy runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap (one-time, in order)

1. **1Password items** (Wolfe.Lab vault): `netlify-pat` (exists) and
   `tofu-state-passphrase` — create as a Password item, e.g.
   `op item create --vault Wolfe.Lab --category password --title tofu-state-passphrase --generate-password=64,letters,digits`.
2. **DNS record**: `scripts/apply.sh caddy` from the repo root.
   Check `lab_ipv4` still matches the mini first, and give the mini a DHCP
   reservation if it doesn't have one — the record is only as stable as
   the address.
3. **First certificate**: run `flows/renew-certs/script.sh` on the mini
   (or the renew-certs workflow); it resolves the Netlify token from the
   vault for the DNS-01 challenge (`flows/renew-certs/secrets.env`).
   Caddy loads the cert from files and cannot START without them —
   setup.sh encodes this ordering.
4. **First deploy**: run the caddy workflow (or
   `docker compose up -d` in this directory on the mini). Must happen
   ONCE before redeploying any proxied slice — this compose creates the
   `lab` network the others reference as external.
5. **Re-up the proxied slices** (forgejo, jellyfin, garage) so
   their containers join the network. Compose recreates them — brief
   downtime each.

If LAN clients can't resolve `*.lab.twolfe.dev` while phones on mobile
data can: that's the router's DNS-rebind protection refusing public names
that resolve to RFC1918 space. Allowlist `twolfe.dev` in the router.

## Upgrading

Caddy: bump the `image:` pin in `compose.yaml`, redeploy. lego: bump the
image tag in `flows/renew-certs/script.sh`, and check the lego release
notes — a major bump can change the CLI (v4→v5 did).
