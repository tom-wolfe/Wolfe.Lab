data "netlify_dns_zone" "twolfe_dev" {
  name = "twolfe.dev"
}

resource "netlify_dns_record" "git" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "A"
  hostname = "git.twolfe.dev"
  value    = var.forgejo_tailscale_ipv4

  lifecycle {
    prevent_destroy = true
  }
}

resource "netlify_dns_record" "code" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "CNAME"
  hostname = "code.twolfe.dev"
  value    = "forgejo.ts.twolfe.dev"

  lifecycle {
    prevent_destroy = true
  }
}
