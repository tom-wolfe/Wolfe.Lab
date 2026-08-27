variable "netlify_token" {
  description = "Netlify personal access token (1P `netlify-pat`; account-wide — Netlify PATs can't be scoped)"
  type        = string
  sensitive   = true
}

variable "state_passphrase" {
  description = "State encryption passphrase (1P `tofu-state-passphrase`) — see encryption.tf"
  type        = string
  sensitive   = true
}

variable "lab_ipv4" {
  description = <<-EOT
    The mini's LAN address — target of the *.lab wildcard record. Must stay
    stable: give the mini a DHCP reservation in the router if it doesn't
    have one. When Tailscale lands, repointing this to the mini's 100.x
    address is the whole remote-access migration.
  EOT
  type        = string
  default     = "192.168.0.7"
}
