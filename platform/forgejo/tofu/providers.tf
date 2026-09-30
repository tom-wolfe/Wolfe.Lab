terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket                      = "tofu-state"
    key                         = "forgejo/terraform.tfstate"
    endpoints                   = { s3 = "http://macmini.local:3900" }
    region                      = "garage"
    use_path_style              = true
    skip_credentials_validation = true
    skip_region_validation      = true
    skip_requesting_account_id  = true
    skip_metadata_api_check     = true
    skip_s3_checksum            = true
    # use_lockfile absent because Garage lacks S3 conditional writes.
  }

  required_providers {
    forgejo = {
      source = "registry.terraform.io/svalabs/forgejo"
    }
    # Push mirrors only. Svalabs doesn't support them.
    adyxax = {
      source  = "registry.terraform.io/adyxax/forgejo"
      version = "~> 1.2"
    }
    netlify = {
      source  = "registry.terraform.io/netlify/netlify"
      version = "~> 0.4"
    }
  }
}

provider "forgejo" {
  host = "http://macmini.local:3000"
}

provider "adyxax" {
  base_uri = "http://macmini.local:3000"
}

provider "netlify" {
  token = var.netlify_token
}
