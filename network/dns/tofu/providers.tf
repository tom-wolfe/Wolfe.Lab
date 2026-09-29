terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket                      = "tofu-state"
    key                         = "dns/terraform.tfstate"
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
    # Same provider, same 1P item as the caddy and forgejo roots — one
    # account-wide PAT, several roots. See README.md for why this root
    # exists at all.
    netlify = {
      # Fully-qualified: OpenTofu defaults to registry.opentofu.org.
      source  = "registry.terraform.io/netlify/netlify"
      version = "~> 0.4"
    }
  }
}

provider "netlify" {
  token = var.netlify_token
}
