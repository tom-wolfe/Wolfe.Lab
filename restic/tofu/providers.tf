terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket                      = "tofu-state"
    key                         = "restic/terraform.tfstate"
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
    b2 = {
      source  = "registry.terraform.io/Backblaze/b2"
      version = "~> 0.10"
    }
    healthchecksio = {
      source  = "registry.terraform.io/kristofferahl/healthchecksio"
      version = "~> 2.3"
    }
  }
}

# The MASTER application key (1P `b2-master-key`) — used only from this
# root, to mint the scoped key below. restic itself never sees it.
provider "b2" {
  application_key_id = var.b2_application_key_id
  application_key    = var.b2_application_key
}

provider "healthchecksio" {
  api_key = var.healthchecks_api_key
}
