# git.twolfe.dev — the SSH clone name compose.yaml advertises via
# SSH_DOMAIN. It points at the tailscale SIDECAR's address, not the
# mini's: that dedicated address is the whole reason container port 22
# is free (see "Tailnet identity" in ../README.md). This is the
# per-service "neat public name in the owning slice's tofu, pointing at
# a Tailscale IP" pattern from caddy/README.md — its first instance.
#
# Deliberately NOT forgejo.lab.twolfe.dev: that name must keep resolving
# to the MINI so the web UI routes through caddy — and port 22 at the
# mini's address is macOS Remote Login, the very conflict that created
# the old :2222. One name resolves to one address; the web name and the
# SSH name point at different machines, so they cannot be the same name.
#
# A public record for a tailnet-only address is the *.ts wildcard's
# shape: RESOLUTION needs the internet (Netlify DNS, like every .lab
# name — the local-DNS-on-a-Pi roadmap item retires that), ROUTING needs
# tailnet membership. The sidecar's MagicDNS name
# (forgejo.tailf823b8.ts.net) is the same endpoint with no DNS
# dependency — the fallback when Netlify is the broken thing.

data "netlify_dns_zone" "twolfe_dev" {
  name = "twolfe.dev"
}

resource "netlify_dns_record" "git" {
  zone_id  = data.netlify_dns_zone.twolfe_dev.id
  type     = "A"
  hostname = "git.twolfe.dev"
  value    = var.forgejo_tailscale_ipv4
}
