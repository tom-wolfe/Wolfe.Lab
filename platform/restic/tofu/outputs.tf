output "bucket_name" {
  description = "The offsite bucket. The repository URL for the 1P `restic-b2` item is s3:https://<s3 endpoint from the B2 UI>/<this>"
  value       = b2_bucket.restic.bucket_name
}

output "restic_application_key_id" {
  description = "Goes in the 1P `restic-b2` item's `username` field"
  value       = b2_application_key.restic.application_key_id
}

output "restic_application_key" {
  description = "Goes in the 1P `restic-b2` item's `credential` field — read with `tofu output -raw restic_application_key`"
  value       = b2_application_key.restic.application_key
  sensitive   = true
}

output "ping_url" {
  description = "The offsite check's ping URL — eyeball against the healthchecks.io UI after an apply"
  value       = healthchecksio_check.restic_offsite.ping_url
}
