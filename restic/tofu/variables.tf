variable "b2_application_key_id" {
  description = "Backblaze B2 MASTER application key id (1P `b2-master-key`, `username` field) — Account -> Application Keys"
  type        = string
  sensitive   = true
}

variable "b2_application_key" {
  description = "Backblaze B2 MASTER application key (1P `b2-master-key`, `credential` field)"
  type        = string
  sensitive   = true
}

variable "healthchecks_api_key" {
  description = "healthchecks.io project API key, read-write (1P `healthchecks-api-key`). Project Settings -> API Access."
  type        = string
  sensitive   = true
}

variable "state_passphrase" {
  description = "State encryption passphrase (1P `tofu-state-passphrase`) — see encryption.tf"
  type        = string
  sensitive   = true
}
