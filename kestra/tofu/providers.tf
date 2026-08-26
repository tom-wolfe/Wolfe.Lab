terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket = "tofu-state"
    # Named for the slice — the key survived the restructure unchanged
    # (this root began life as tofu/kestra and ended up back home).
    key                         = "kestra/terraform.tfstate"
    endpoints                   = { s3 = "http://macmini.local:3900" }
    region                      = "garage"
    use_path_style              = true
    skip_credentials_validation = true
    skip_region_validation      = true
    skip_requesting_account_id  = true
    skip_metadata_api_check     = true
    skip_s3_checksum            = true
    # use_lockfile deliberately absent — Garage lacks S3 conditional writes.
    # Solo operator: never run applies from two machines at once.
  }

  required_providers {
    kestra = {
      # Fully-qualified: OpenTofu defaults to registry.opentofu.org.
      source  = "registry.terraform.io/kestra-io/kestra"
      version = "~> 1.3"
    }
  }
}

# Basic-auth credentials come from TF_VAR_* in secrets.env (via op run).
# tenant_id is EE-only — OSS is single-tenant ("main"), nothing to set.
provider "kestra" {
  url      = "http://macmini.local:8180"
  username = var.kestra_username
  password = var.kestra_password
}
