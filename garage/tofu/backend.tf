# The state store every other tofu project backends onto. This project's own
# state is deliberately LOCAL and disposable: run once, store the outputs in
# 1Password, delete terraform.tfstate. See README.md.
#
# (No versioning block, unlike the AWS equivalent — Garage doesn't support
# object versioning yet. State history comes from backups instead.)

resource "garage_bucket" "tofu_state" {
  global_alias = "tofu-state"
}

resource "garage_key" "tofu_state" {
  name = "tofu-state-key"
}

resource "garage_bucket_key" "tofu_state" {
  bucket_id     = garage_bucket.tofu_state.id
  access_key_id = garage_key.tofu_state.access_key_id
  read          = true
  write         = true
  owner         = false
}
