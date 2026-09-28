terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket                      = "tofu-state"
    key                         = "gatus/terraform.tfstate"
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
    netlify = {
      source  = "registry.terraform.io/netlify/netlify"
      version = "~> 0.4"
    }
  }
}

provider "netlify" {
  token = var.netlify_token
}
