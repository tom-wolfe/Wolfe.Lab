terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket = "tofu-state"
    # The service was carved out of chezmoi/; the key stays, because moving state is not a rename.
    key                         = "chezmoi/terraform.tfstate"
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
    # healthchecks.io is a PROVIDER, not a lab service — same call as netlify
    # (see network/caddy/tofu/providers.tf). A check belongs to the service that owns
    # the thing being checked, so the tick's check lives here rather than in
    # a monitoring/ service that would collect other services' concerns.
    healthchecksio = {
      # Fully-qualified: OpenTofu defaults to registry.opentofu.org.
      source  = "registry.terraform.io/kristofferahl/healthchecksio"
      version = "~> 2.3"
    }
  }
}

provider "healthchecksio" {
  api_key = var.healthchecks_api_key
}
