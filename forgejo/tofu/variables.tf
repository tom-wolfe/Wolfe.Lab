variable "forgejo_owner" {
  description = "Forgejo user that owns all mirrored repositories"
  type        = string
  default     = "tom-wolfe"
}

variable "mirror_interval" {
  description = "Pull-mirror sync interval"
  type        = string
  default     = "8h0m0s"
}

# Fine-grained PATs, one per GitHub owner that has PRIVATE repos
# Public-only owners need no token.

variable "github_token_tom_wolfe" {
  type      = string
  sensitive = true
}

variable "github_token_nschema_org" {
  type      = string
  sensitive = true
}

variable "github_token_disastercare" {
  type      = string
  sensitive = true
}

variable "state_passphrase" {
  description = "State encryption passphrase"
  type        = string
  sensitive   = true
}
