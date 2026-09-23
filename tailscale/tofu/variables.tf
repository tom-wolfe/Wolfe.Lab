variable "tailscale_oauth_client_id" {
  description = "OAuth client id (1P `tailscale-oauth`, `username`) — scopes policy_file, devices:core, dns; tag:server (which owns tag:hybrid)"
  type        = string
  sensitive   = true
}

variable "tailscale_oauth_client_secret" {
  description = "OAuth client secret (1P `tailscale-oauth`, `credential`)"
  type        = string
  sensitive   = true
}

variable "state_passphrase" {
  description = "State encryption passphrase (1P `tofu-state-passphrase`) — see encryption.tf"
  type        = string
  sensitive   = true
}

variable "tailnet_dns_suffix" {
  description = "The tailnet's MagicDNS suffix; devices are looked up by full name"
  type        = string
  default     = "tailf823b8.ts.net"
}

variable "servers" {
  description = <<-EOT
    Machine names of the always-on nodes: tagged tag:server and their key
    expiry disabled. Adding a node is adding it here. The forgejo entry is
    the sidecar (forgejo/README.md "Tailnet identity").
  EOT
  type        = list(string)
  default     = ["macmini", "wolfe-pi5", "forgejo"]
}

variable "hybrids" {
  description = <<-EOT
    Machine names of the hybrid nodes — workstations that also serve while
    they are on (ROADMAP.md #7): tagged tag:hybrid and their key expiry
    disabled, exactly as a server is, but reachable only where the policy
    says.
  EOT
  type        = list(string)
  default     = ["macstudio"]
}
