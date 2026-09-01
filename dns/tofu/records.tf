#
# Proton Mail DNS Records
#

data "netlify_dns_zone" "twolfe_dev" {
  name = "twolfe.dev"
}

# --- Delivery -----------------------------------------------------------

resource "netlify_dns_record" "mx_primary" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "MX"
  hostname = "twolfe.dev"
  value    = "mail.protonmail.ch"
  priority = 10
  ttl      = 300

  lifecycle {
    prevent_destroy = true
  }
}

resource "netlify_dns_record" "mx_secondary" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "MX"
  hostname = "twolfe.dev"
  value    = "mailsec.protonmail.ch"
  priority = 20
  ttl      = 300

  lifecycle {
    prevent_destroy = true
  }
}

# --- Authentication -----------------------------------------------------

resource "netlify_dns_record" "spf" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "TXT"
  hostname = "twolfe.dev"
  value    = "v=spf1 include:_spf.protonmail.ch ~all"
  ttl      = 300

  lifecycle {
    prevent_destroy = true
  }
}

resource "netlify_dns_record" "dmarc" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "TXT"
  hostname = "_dmarc.twolfe.dev"
  value    = "v=DMARC1; p=quarantine"
  ttl      = 300

  lifecycle {
    prevent_destroy = true
  }
}

# DKIM
resource "netlify_dns_record" "dkim1" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "CNAME"
  hostname = "protonmail._domainkey.twolfe.dev"
  value    = "protonmail.domainkey.ddpk3qgd2ydshnjdut2egnlcv7j2lhxaguz323lcnx7k3etpfzfmq.domains.proton.ch."
  ttl      = 300

  lifecycle {
    prevent_destroy = true
  }
}

resource "netlify_dns_record" "dkim2" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "CNAME"
  hostname = "protonmail2._domainkey.twolfe.dev"
  value    = "protonmail2.domainkey.ddpk3qgd2ydshnjdut2egnlcv7j2lhxaguz323lcnx7k3etpfzfmq.domains.proton.ch."
  ttl      = 300

  lifecycle {
    prevent_destroy = true
  }
}

resource "netlify_dns_record" "dkim3" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "CNAME"
  hostname = "protonmail3._domainkey.twolfe.dev"
  value    = "protonmail3.domainkey.ddpk3qgd2ydshnjdut2egnlcv7j2lhxaguz323lcnx7k3etpfzfmq.domains.proton.ch."
  ttl      = 300

  lifecycle {
    prevent_destroy = true
  }
}

# --- Ownership ----------------------------------------------------------

resource "netlify_dns_record" "protonmail_verification" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "TXT"
  hostname = "twolfe.dev"
  value    = "protonmail-verification=97bf96e13fb4e467dc0e0c7590e32dc93a2f2ad7"
  ttl      = 300

  lifecycle {
    prevent_destroy = true
  }
}
