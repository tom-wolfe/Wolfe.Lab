variable "paperless_api_token" {
  description = "Paperless API token for the admin user (1P `paperless-api-token`). Profile -> API Auth Token in the UI."
  type        = string
  sensitive   = true
}

variable "bridge_username" {
  description = "Proton Bridge IMAP username (1P `proton-bridge`) — the same login the mail watcher uses."
  type        = string
  sensitive   = true
}

variable "bridge_password" {
  description = "Proton Bridge IMAP password (1P `proton-bridge`) — Bridge's generated one, not the Proton account's."
  type        = string
  sensitive   = true
}

variable "state_passphrase" {
  description = "State encryption passphrase (1P `tofu-state-passphrase`) — see encryption.tf"
  type        = string
  sensitive   = true
}
