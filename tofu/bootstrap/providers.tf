terraform {
  required_version = ">= 1.10"

  required_providers {
    garage = {
      # Fully-qualified: this provider is on the Terraform registry only,
      # and OpenTofu defaults to registry.opentofu.org.
      source  = "registry.terraform.io/Arsolitt/garagehq"
      version = "~> 1.0"
    }
  }
}

provider "garage" {
  host   = var.garage_host
  scheme = "http"
  token  = var.garage_admin_token
}
