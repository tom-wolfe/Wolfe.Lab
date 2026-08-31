# The Proton Mail set, hand-made during the 2026-08 mail migration and
# imported here (see imports.tf) rather than recreated — a wrong mail
# record fails silently with a days-long feedback loop. Values were
# captured from the authoritative nameserver (dig @dns1.p04.nsone.net)
# on 2026-08-31 and must match the zone exactly: the first plan proves
# it by showing import-only, zero changes. See README.md.
#
# ttl = 300 throughout because that is what the Proton setup created —
# declared, not defaulted, so a UI hand-edit surfaces as drift.

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
}

resource "netlify_dns_record" "mx_secondary" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "MX"
  hostname = "twolfe.dev"
  value    = "mailsec.protonmail.ch"
  priority = 20
  ttl      = 300
}

# --- Authentication -----------------------------------------------------

resource "netlify_dns_record" "spf" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "TXT"
  hostname = "twolfe.dev"
  value    = "v=spf1 include:_spf.protonmail.ch ~all"
  ttl      = 300
}

resource "netlify_dns_record" "dmarc" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "TXT"
  hostname = "_dmarc.twolfe.dev"
  value    = "v=DMARC1; p=quarantine"
  ttl      = 300
}

# DKIM is three CNAMEs into domains.proton.ch by design: Proton rotates
# the actual signing keys on its side of the pointer, so these never
# need to change for a rotation. The opaque label is per-domain and
# stable.
resource "netlify_dns_record" "dkim1" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "CNAME"
  hostname = "protonmail._domainkey.twolfe.dev"
  value    = "protonmail.domainkey.ddpk3qgd2ydshnjdut2egnlcv7j2lhxaguz323lcnx7k3etpfzfmq.domains.proton.ch."
  ttl      = 300
}

resource "netlify_dns_record" "dkim2" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "CNAME"
  hostname = "protonmail2._domainkey.twolfe.dev"
  value    = "protonmail2.domainkey.ddpk3qgd2ydshnjdut2egnlcv7j2lhxaguz323lcnx7k3etpfzfmq.domains.proton.ch."
  ttl      = 300
}

resource "netlify_dns_record" "dkim3" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "CNAME"
  hostname = "protonmail3._domainkey.twolfe.dev"
  value    = "protonmail3.domainkey.ddpk3qgd2ydshnjdut2egnlcv7j2lhxaguz323lcnx7k3etpfzfmq.domains.proton.ch."
  ttl      = 300
}

# --- Ownership ----------------------------------------------------------

resource "netlify_dns_record" "protonmail_verification" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "TXT"
  hostname = "twolfe.dev"
  value    = "protonmail-verification=97bf96e13fb4e467dc0e0c7590e32dc93a2f2ad7"
  ttl      = 300
}
