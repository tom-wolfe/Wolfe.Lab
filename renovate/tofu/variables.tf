variable "renovate_password" {
  description = "Password of the renovate user (1P `forgejo-renovate`), which forgejo/tofu creates: the provider logs in as it to mint its token"
  type        = string
  sensitive   = true
}

variable "renovate_github_token" {
  description = "Read-only GitHub token for Renovate's lookups on github.com (1P `github-renovate-pat`; fine-grained, public repositories, no permissions)"
  type        = string
  sensitive   = true
}

variable "state_passphrase" {
  description = "State encryption passphrase (1P `tofu-state-passphrase`) — see encryption.tf"
  type        = string
  sensitive   = true
}
