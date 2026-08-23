# Garage

S3-compatible object storage on the Mac mini. First customer: OpenTofu state.
Future customers: backups, artifacts, anything S3-shaped.

State lives at `~/Docker/garage/{meta,data}` on the mini; config is
`garage.toml` here (mounted read-only, secret-free). Everything below is
automated by chezmoi — bootstrap is just: merge, `chezmoi update` on the mini.

| Concern | Handled by |
| --- | --- |
| Secrets (`~/Docker/garage/garage.env`) | `run_once_before_bootstrap-garage-secrets` |
| Container | `run_onchange_after_deploy-compose` (like every stack) |
| Cluster layout (one-time) | `run_once_after_init-garage-layout` |
| Buckets, keys, grants | OpenTofu — `tofu/bootstrap` seeds the state store; everything else is ordinary tofu resources |

**Adding a bucket** = a `garage_bucket` (+ `garage_key` + `garage_bucket_key`)
resource in the relevant tofu project. Storage configuration lives in tofu;
chezmoi only bootstraps what must exist before the admin API answers.

## Operational notes

- `garage.toml` changes are NOT picked up by `docker compose up -d` (it's a
  bind mount) — after editing, restart:
  `ssh macmini "docker compose --project-directory ~/.local/share/chezmoi/compose/garage restart"`
- Health/audit: `docker exec garage /garage stats` / `bucket list` / `key list`.
- Backup = `meta/` (small, critical) + `data/` (the objects). Cold-copy
  pattern as per forgejo's backup.sh; scheduling TODO.

## Upgrading

Read the release notes first — metadata formats migrate and downgrades are
not supported. Bump the image tag, merge, `chezmoi update`.
