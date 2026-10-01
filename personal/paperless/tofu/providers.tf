terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket                      = "tofu-state"
    key                         = "paperless/terraform.tfstate"
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
    # Paperless has no provider of its own, so its REST API is driven directly.
    restapi = {
      source  = "registry.terraform.io/Mastercard/restapi"
      version = "~> 3.0"
    }
  }
}

provider "restapi" {
  uri = "https://paperless.twolfe.dev"

  headers = {
    Authorization = "Token ${var.paperless_api_token}"
    Content-Type  = "application/json"
  }

  id_attribute         = "id"
  write_returns_object = true
  update_method        = "PATCH"
}
