terraform {
  required_version = ">= 1.10"

  backend "s3" {
    bucket                      = "tofu-state"
    key                         = "renovate/terraform.tfstate"
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
  }
}

# The owner's token (FORGEJO_API_TOKEN): writes the repository's secrets.
provider "forgejo" {
  host = "http://macmini.local:3000"
}

# Renovate itself, over basic auth: Forgejo will not create a token for a
# request authenticated with a token. The provider connects as soon as it is
# configured, so the user must already exist — forgejo/tofu creates it, and
# applies first.
provider "forgejo" {
  alias    = "renovate"
  host     = "http://macmini.local:3000"
  username = "renovate"
  password = var.renovate_password
  # FORGEJO_API_TOKEN would otherwise apply here too, and the provider
  # refuses a username and a token together.
  api_token = ""
}
