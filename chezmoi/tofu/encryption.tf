# Client-side state encryption — see caddy/tofu/encryption.tf for the full
# rationale (Garage has no SSE; state holds secrets; passphrase is 1P
# `tofu-state-passphrase` via TF_VAR in secrets.env). This root was born
# encrypted, so `enforced = true` from day one — no migration shape here.
terraform {
  encryption {
    key_provider "pbkdf2" "state" {
      passphrase = var.state_passphrase
    }

    method "aes_gcm" "state" {
      keys = key_provider.pbkdf2.state
    }

    state {
      method   = method.aes_gcm.state
      enforced = true
    }

    plan {
      method   = method.aes_gcm.state
      enforced = true
    }
  }
}
