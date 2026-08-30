# The front door's DNS: TWO wildcard records covering every internal
# hostname — the same door, reached two ways. *.lab points at the mini's
# LAN address and works for anything in the house, tailnet or not; *.ts
# points at its Tailscale address and works wherever the tailnet is,
# hotspot laptop included. Caddy owns both because every name under
# either wildcard terminates at Caddy — these are records for the front
# door, not for any service.
#
# Decision 2026-08-31 (Tom's): this REPLACES the roadmap's original plan
# of repointing *.lab at the Tailscale address, which would have cut off
# non-tailnet LAN devices (a TV Jellyfin app, guests). Two names, every
# device has an option. Both wildcards collapse back into one if the
# local-DNS-on-a-Pi item (ROADMAP.md) ever lands.
#
# Per-service records exist only for deliberate PUBLIC exposure
# (jellyfin.twolfe.dev, code.twolfe.dev, ...) and live in the OWNING
# slice's tofu root, not here.
#
# NB: a public record pointing at RFC1918 space is deliberate and
# harmless — useless off the LAN, standard for homelabs. If names resolve
# everywhere except ON the LAN, that's the router's DNS-rebind protection:
# allowlist twolfe.dev there, not here.

data "netlify_dns_zone" "twolfe_dev" {
  name = "twolfe.dev"
}

resource "netlify_dns_record" "lab_wildcard" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "A"
  hostname = "*.lab.twolfe.dev"
  value    = var.lab_ipv4
}

resource "netlify_dns_record" "ts_wildcard" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "A"
  hostname = "*.ts.twolfe.dev"
  value    = var.lab_tailscale_ipv4
}
