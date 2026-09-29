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
