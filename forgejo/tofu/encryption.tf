# Client-side state encryption — see caddy/tofu/encryption.tf for the full
# rationale (Garage has no SSE; state holds secrets; passphrase is 1P
# `tofu-state-passphrase` via TF_VAR in secrets.env). Migrated from
# plaintext 2026-08-28; `enforced` means tofu now refuses to read OR write
# unencrypted state here — a wrong/missing passphrase fails loudly instead
# of silently rewriting plaintext.
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
