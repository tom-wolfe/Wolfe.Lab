variable "garage_host" {
  description = "Garage admin API endpoint (host:port, no scheme)"
  type        = string
  default     = "macmini.local:3903"
}

variable "garage_admin_token" {
  description = "Garage admin token (GARAGE_ADMIN_TOKEN from the mini's garage.env; keep in 1Password)"
  type        = string
  sensitive   = true
}
