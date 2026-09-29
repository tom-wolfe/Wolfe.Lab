# The tailnet policy file, verbatim from policy.hujson.
resource "tailscale_acl" "lab" {
  acl = file("${path.module}/policy.hujson")
}

# MagicDNS (<host>.tailf823b8.ts.net), declared.
resource "tailscale_dns_preferences" "lab" {
  magic_dns = true
}
