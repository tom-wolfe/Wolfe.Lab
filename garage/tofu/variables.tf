variable "garage_host" {
  description = "Garage admin API endpoint (host:port, no scheme)"
  type        = string
  default     = "macmini.local:3903"
}

variable "garage_admin_token" {
  description = "Garage admin token"
  type        = string
  sensitive   = true
}
