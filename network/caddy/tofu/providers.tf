terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket                      = "tofu-state"
    key                         = "caddy/terraform.tfstate"
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
    # Netlify's own current provider (the archived hashicorp/netlify one is
    # dead). This is a PROVIDER, not a slice: any slice that needs a DNS
    # record configures it and declares its own records — this root owns
    # only the front door's wildcard. See README.md.
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
