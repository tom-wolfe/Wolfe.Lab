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
    # use_lockfile deliberately absent — Garage lacks S3 conditional writes.
    # Solo operator: never run applies from two machines at once.
  }

  required_providers {
    # Repositories (pull mirrors, active repos, the primary)
    forgejo = {
      source = "registry.terraform.io/svalabs/forgejo"
    }
    # Push mirrors only — svalabs doesn't model them
    adyxax = {
      source  = "registry.terraform.io/adyxax/forgejo"
      version = "~> 1.2"
    }
    # The git.twolfe.dev clone-name record (records.tf). A PROVIDER, not
    # a slice: per-service DNS lives in the owning slice's root
    # (caddy/README.md) — this is that rule's first per-service use.
    netlify = {
      # Fully-qualified: OpenTofu defaults to registry.opentofu.org.
      source  = "registry.terraform.io/netlify/netlify"
      version = "~> 0.4"
    }
  }
}

# Both providers read FORGEJO_API_TOKEN from the environment (via op run).
provider "forgejo" {
  host = "http://macmini.local:3000"
}

provider "adyxax" {
  base_uri = "http://macmini.local:3000"
}

provider "netlify" {
  token = var.netlify_token
}
