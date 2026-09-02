data "netlify_dns_zone" "twolfe_dev" {
  name = "twolfe.dev"
}

resource "netlify_dns_record" "status" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "CNAME"
  hostname = "status.twolfe.dev"
  value    = "gatus.ts.twolfe.dev"

  lifecycle {
    prevent_destroy = true
  }
}
