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

variable "netlify_token" {
  description = "Netlify personal access token (1P `netlify-pat`; account-wide — Netlify PATs can't be scoped)"
  type        = string
  sensitive   = true
}

variable "forgejo_tailscale_ipv4" {
  description = <<-EOT
    The forgejo SIDECAR's Tailscale address — target of the
    git.twolfe.dev record (records.tf). Unknowable until the sidecar
    first enrols: read it from the admin console or `tailscale status`,
    then write it in as this variable's default and commit (caddy's
    lab_tailscale_ipv4 pattern). Until then every plan prompts for it —
    a deliberate tripwire saying the record isn't real yet. Stable for
    the life of the node key (bootstrap disables key expiry on the
    forgejo node, same as the mini); a re-enrolment mints a new address,
    which means updating this and re-applying.
  EOT
  type        = string
}
