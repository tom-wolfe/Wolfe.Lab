output "access_key_id" {
  description = "S3 access key ID for the tofu-state bucket"
  value       = garage_key.tofu_state.access_key_id
}

output "secret_access_key" {
  description = "S3 secret key — retrieve with `tofu output -raw secret_access_key`"
  value       = garage_key.tofu_state.secret_access_key
  sensitive   = true
}
