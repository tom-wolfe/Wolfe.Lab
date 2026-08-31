# Client-side state encryption (OpenTofu 1.7+). Garage has no server-side
# encryption and recommends rolling your own — this is that, done in tofu
# itself: state and plan files are AES-GCM encrypted BEFORE they reach the
# backend, keyed from a passphrase via PBKDF2.
#
# The passphrase is 1Password item `tofu-state-passphrase` (one shared
# passphrase across all slices, like the one netlify-pat), injected as
# TF_VAR_state_passphrase by `op run` — see secrets.env. Losing it means
# losing the STATE, not the infrastructure: everything here can be
# re-imported. Rotation: add the new passphrase as a second key provider,
# move the old one to a fallback block, apply once, remove the fallback.
#
# This root was born encrypted, so `enforced = true` from day one. For
# retrofitted slices the migration shape is different — see the forgejo or
# kestra encryption.tf.
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
