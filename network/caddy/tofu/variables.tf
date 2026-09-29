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

variable "lab_tailscale_ipv4" {
  description = <<-EOT
    The mini's Tailscale address — target of the wildcard and of the front
    door's own record, and so of every name the lab answers to.
    Stable for the life of the node key (disabling key expiry on the mini
    is part of the tailscale slice's bootstrap, and this variable is why
    it matters); a re-enrolment that mints a new address means updating
    this and re-applying.
  EOT
  type        = string
  default     = "100.89.210.32"
}
