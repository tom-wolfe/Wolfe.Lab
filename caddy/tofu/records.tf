# The front door's DNS: ONE wildcard record covering every internal
# hostname. Caddy owns it because every name under *.lab terminates at
# Caddy — it's the record for the front door, not for any service.
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
