# Garage

S3-compatible object storage on the Mac mini. First customer: OpenTofu state.
Future customers: backups, artifacts, anything S3-shaped.

State lives at `~/Docker/garage/{meta,data}` on the mini; config is
`garage.toml` here (mounted read-only, secret-free).

| Concern | Handled by |
| --- | --- |
| Secrets | `secrets.env` names the vault items (`garage-rpc-secret`, `garage-s3-admin-token`); the deploy resolves them into the environment of the `up` that creates the container. Never generated: the vault is the origin, so a recreated container gets the same values back. Nothing is on disk — bring the stack up with `lab deploy`, never a bare `compose up` |
| Container | `.forgejo/workflows/garage-compose.yaml` on every push that touches `compose/` (the mini's host runner); first bring-up via `setup.sh` |
| Cluster layout (one-time) | `lab init` from `layout/` (its `ritten.json` names the zone and capacity), invoked by `setup.sh`; a no-op once a layout exists |
| Buckets, keys, grants | OpenTofu — this slice's `tofu/` seeds the state store (below); everything else is ordinary tofu resources |

**Adding a bucket** = a `garage_bucket` (+ `garage_key` + `garage_bucket_key`)
resource in the relevant tofu project. Storage configuration lives in tofu;
bootstrap only creates what must exist before the admin API answers.

## Operational notes

- `garage.toml` changes are NOT picked up by `docker compose up -d` (it's a
  bind mount) — after editing, restart:
  `ssh macmini "docker compose --project-directory .local/share/Wolfe.Lab/garage restart"`
- Health/audit: `docker exec garage /garage stats` / `bucket list` / `key list`.
- Backup = `meta/` (small, critical) + `data/` (the objects): the
  nightly `garage-backup.yaml` (`lab backup` from `backup/`) does a cold copy at 02:30
  — stop, restic snapshot of both, start; `restic-offsite.yaml` ships it
  to B2 and owns retention (`restic/README.md`); refuses to run if the
  drive isn't mounted. The secrets are not on disk to include — the
  vault is their origin.

## The tofu state store (`tofu/`)

Creates the OpenTofu state store (bucket `tofu-state` + key + grant) on
Garage — the chicken that lays every other project's egg. Its own state is
deliberately **local and disposable**: run once, harvest the outputs, delete
the state.

The root carries a `ritten.json` like every other, so `lab check` and
`lab deploy` run from it; it has no workflow, because its state is local
and disposable and a plan on a runner would have nothing to read.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
