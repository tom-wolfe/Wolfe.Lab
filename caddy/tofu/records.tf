# The front door's DNS.

data "netlify_dns_zone" "twolfe_dev" {
  name = "twolfe.dev"
}

resource "netlify_dns_record" "wildcard" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "A"
  hostname = "*.twolfe.dev"
  value    = var.lab_tailscale_ipv4

  lifecycle {
    prevent_destroy = true
  }
}

# The door's own name to the tailnet.
resource "netlify_dns_record" "front_door" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "A"
  hostname = "lab.twolfe.dev"
  value    = var.lab_tailscale_ipv4

  lifecycle {
    prevent_destroy = true
  }
}
