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
