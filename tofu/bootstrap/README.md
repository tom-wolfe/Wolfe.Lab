# bootstrap

Creates the OpenTofu state store (bucket `tofu-state` + key + grant) on
Garage — the chicken that lays every other project's egg. Its own state is
local and disposable: run once, harvest the outputs, delete the state.

## Run once

```sh
# Admin token: generated on the mini by chezmoi (bootstrap-garage-secrets),
# stored in 1Password. First time: ssh macmini cat ~/Docker/garage/garage.env
export TF_VAR_garage_admin_token=...   # or inject via `op run`

tofu init
tofu apply

tofu output access_key_id
tofu output -raw secret_access_key     # -> 1Password item "tofu-state-key"

rm -rf terraform.tfstate terraform.tfstate.backup
```

Every other project under `tofu/` then uses the `s3` backend against
`http://macmini.local:3900` with those credentials.

## Re-running later

With the state deleted, a re-apply would try to create resources that
already exist and fail on the alias conflict. To manage these resources
again (e.g. to add grants), re-adopt them with `import` blocks instead of
recreating — or just do one-off changes via `docker exec garage /garage`.
