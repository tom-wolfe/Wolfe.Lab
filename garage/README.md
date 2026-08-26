# Garage

S3-compatible object storage on the Mac mini. First customer: OpenTofu state.
Future customers: backups, artifacts, anything S3-shaped.

State lives at `~/Docker/garage/{meta,data}` on the mini; config is
`garage.toml` here (mounted read-only, secret-free).

| Concern | Handled by |
| --- | --- |
| Secrets (`~/Docker/garage/garage.env`) | `run_once_before_bootstrap-garage-secrets` (chezmoi) — materialized from 1Password (`Garage RPC Secret`, `Garage S3 Admin Token`), never generated: the vault is the origin, so a wiped env file comes back with the same values |
| Container | the `deploy-garage` flow (`flows/deploy/flow.yaml`), chained on the chezmoi tick like every stack; first bring-up via `setup.sh` |
| Cluster layout (one-time) | `scripts/init-layout.sh`, invoked by `setup.sh` |
| Buckets, keys, grants | OpenTofu — this slice's `tofu/` seeds the state store (below); everything else is ordinary tofu resources |

**Adding a bucket** = a `garage_bucket` (+ `garage_key` + `garage_bucket_key`)
resource in the relevant tofu project. Storage configuration lives in tofu;
bootstrap only creates what must exist before the admin API answers.

## Operational notes

- `garage.toml` changes are NOT picked up by `docker-compose up -d` (it's a
  bind mount) — after editing, restart:
  `ssh macmini "docker-compose --project-directory .local/share/chezmoi/garage restart"`
- Health/audit: `docker exec garage /garage stats` / `bucket list` / `key list`.
- Backup = `meta/` (small, critical) + `data/` (the objects). Cold-copy
  pattern as per forgejo's backup.sh; scheduling TODO.

## Upgrading

Read the release notes first — metadata formats migrate and downgrades are
not supported. Bump the image tag, merge; the tick ships it and
`deploy-garage` converges it.

## The tofu state store (`tofu/`)

Creates the OpenTofu state store (bucket `tofu-state` + key + grant) on
Garage — the chicken that lays every other project's egg. Its own state is
deliberately **local and disposable**: run once, harvest the outputs, delete
the state.

### Run once

```sh
# Admin token: generated on the mini by chezmoi (bootstrap-garage-secrets),
# stored in 1Password. First time: ssh macmini cat ~/Docker/garage/garage.env
export TF_VAR_garage_admin_token=...   # or inject via `op run`

cd tofu
tofu init
tofu apply

tofu output access_key_id
tofu output -raw secret_access_key     # -> 1Password item "tofu-state-key"

rm -rf terraform.tfstate terraform.tfstate.backup
```

Every other tofu root in the repo (`forgejo/tofu`,
`kestra/tofu`) then uses the `s3` backend against
`http://macmini.local:3900` with those credentials.

### Re-running later

With the state deleted, a re-apply would try to create resources that
already exist and fail on the alias conflict. To manage these resources
again (e.g. to add grants), re-adopt them with `import` blocks instead of
recreating — or just do one-off changes via `docker exec garage /garage`.
