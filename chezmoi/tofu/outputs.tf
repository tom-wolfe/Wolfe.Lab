output "chezmoi_tick_ping_url" {
  description = <<-EOT
    The check's UUID ping URL. NOT what the flow uses — the flow composes
    the slug form from the project ping key (see checks.tf). Exposed only so
    an apply can be eyeballed against the healthchecks.io UI.
  EOT
  value       = healthchecksio_check.chezmoi_tick.ping_url
  sensitive   = true
}
