# Client-side state encryption — see caddy/tofu/encryption.tf for the full
# rationale (Garage has no SSE; state holds secrets; passphrase is 1P
# `tofu-state-passphrase` via TF_VAR in secrets.env).
#
# MIGRATION SHAPE: this root predates encryption, so the state in Garage is
# still plaintext. The unencrypted fallback below lets tofu READ it; the
# first state-writing operation (any apply — flows are safe to re-apply)
# rewrites it encrypted. Once that has happened (check: the object in the
# tofu-state bucket becomes a JSON envelope with an "encryption" key),
# delete the fallback and the unencrypted method, and set `enforced = true`
# on both state and plan — match caddy/tofu/encryption.tf. `enforced` can't
# coexist with an unencrypted fallback, which is why it's absent here.
terraform {
  encryption {
    key_provider "pbkdf2" "state" {
      passphrase = var.state_passphrase
    }

    method "aes_gcm" "state" {
      keys = key_provider.pbkdf2.state
    }

    method "unencrypted" "migrate" {}

    state {
      method = method.aes_gcm.state
    }

    plan {
      method = method.aes_gcm.state
    }
  }
}
