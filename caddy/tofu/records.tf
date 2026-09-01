# The front door's DNS: TWO wildcard records covering every internal
# hostname in duplicate:
# *.lab points at the mini's LAN address and works for anything in the house
# *.ts points at its Tailscale address and works wherever the tailnet is.

data "netlify_dns_zone" "twolfe_dev" {
  name = "twolfe.dev"
}

resource "netlify_dns_record" "lab_wildcard" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "A"
  hostname = "*.lab.twolfe.dev"
  value    = var.lab_ipv4

  lifecycle {
    prevent_destroy = true
  }
}

resource "netlify_dns_record" "ts_wildcard" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "A"
  hostname = "*.ts.twolfe.dev"
  value    = var.lab_tailscale_ipv4

  lifecycle {
    prevent_destroy = true
  }
}
