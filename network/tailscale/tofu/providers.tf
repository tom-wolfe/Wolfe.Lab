terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket                      = "tofu-state"
    key                         = "tailscale/terraform.tfstate"
    endpoints                   = { s3 = "http://macmini.local:3900" }
    region                      = "garage"
    use_path_style              = true
    skip_credentials_validation = true
    skip_region_validation      = true
    skip_requesting_account_id  = true
    skip_metadata_api_check     = true
    skip_s3_checksum            = true
  }

  required_providers {
    tailscale = {
      # First-party. Fully-qualified: OpenTofu defaults to registry.opentofu.org.
      source  = "registry.terraform.io/tailscale/tailscale"
      version = "~> 0.29"
    }
  }
}

# An OAuth client, not a personal API key, which would expire after 90 days.
provider "tailscale" {
  oauth_client_id     = var.tailscale_oauth_client_id
  oauth_client_secret = var.tailscale_oauth_client_secret
}
