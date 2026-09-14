# The always-on nodes, by MagicDNS name.
data "tailscale_device" "server" {
  for_each = toset(var.servers)
  name     = "${each.key}.${var.tailnet_dns_suffix}"
}

# The tag is defined in the policy first (tagOwners); tagging a device
# with an undefined tag is refused, hence depends_on. Tagging an existing
# device keeps its address — caddy/tofu and forgejo/tofu carry those
# addresses as record targets.
resource "tailscale_device_tags" "server" {
  for_each  = data.tailscale_device.server
  device_id = each.value.node_id
  tags      = ["tag:server"]

  depends_on = [tailscale_acl.lab]
}

# A server whose node key silently expires drops off the tailnet with
# nothing to notice. Tailscale disables expiry for tagged devices by
# default; this makes it declared rather than defaulted.
resource "tailscale_device_key" "server" {
  for_each            = data.tailscale_device.server
  device_id           = each.value.node_id
  key_expiry_disabled = true
}
