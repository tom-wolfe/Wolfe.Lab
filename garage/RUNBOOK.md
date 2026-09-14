# Garage runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Run once

```sh
# Admin token: 1Password `garage-s3-admin-token`.
export TF_VAR_garage_admin_token=...

cd tofu
tofu init
tofu apply

tofu output access_key_id
tofu output -raw secret_access_key     # -> 1Password item "tofu-state-key"

rm -rf terraform.tfstate terraform.tfstate.backup
```

Every other tofu root in the repo (`forgejo/tofu`,
`caddy/tofu`, …) then uses the `s3` backend against
`http://macmini.local:3900` with those credentials.

## Re-running later

With the state deleted, a re-apply would try to create resources that
already exist and fail on the alias conflict. To manage these resources
again (e.g. to add grants), re-adopt them with `import` blocks instead of
recreating — or just do one-off changes via `docker exec garage /garage`.

## Upgrading

Read the release notes first — metadata formats migrate and downgrades are
not supported. Bump the image tag, merge; the push deploys it.
