terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket                      = "tofu-state"
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
    # healthchecks.io is a PROVIDER, not a slice — same call as netlify
    # (see caddy/tofu/providers.tf). A check belongs to the slice that owns
    # the thing being checked, so the tick's check lives here rather than in
    # a monitoring/ slice that would collect other slices' concerns.
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
