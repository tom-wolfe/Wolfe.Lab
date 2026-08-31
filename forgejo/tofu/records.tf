data "netlify_dns_zone" "twolfe_dev" {
  name = "twolfe.dev"
}

resource "netlify_dns_record" "git" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "A"
  hostname = "git.twolfe.dev"
  value    = var.forgejo_tailscale_ipv4
}
