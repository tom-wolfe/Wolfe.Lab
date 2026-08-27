variable "kestra_username" {
  description = "Kestra basic-auth username (the login email)"
  type        = string
}

variable "kestra_password" {
  description = "Kestra basic-auth password"
  type        = string
  sensitive   = true
}

variable "state_passphrase" {
  description = "State encryption passphrase"
  type        = string
  sensitive   = true
}
