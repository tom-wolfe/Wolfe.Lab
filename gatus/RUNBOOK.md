# Gatus runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap (one-time, in order)

1. **Materialize the env file** on the mini: `chezmoi apply` (needs
   `OP_SERVICE_ACCOUNT_TOKEN` in a non-login shell; `.zprofile` exports
   it on servers). Confirm `~/Docker/gatus/gatus.env` has two lines.
   No red-tick trap here — the `pushover` item already exists.
2. **Bring the container up**: `./setup.sh` from the repo root, or on the
   mini `docker compose --project-directory <this dir> up -d`. Caddy must
   already have been deployed once (it owns the `lab` network).
3. **Re-issue the certificate** so it carries `status.twolfe.dev` (and
   `code.twolfe.dev`, added in the same change) — `caddy/README.md`
   "Neat names", step 4. At the desk. Until then the `.lab` and `.ts`
   names work and the neat name doesn't.
4. **Reload caddy's routes** — the caddy workflow (it fires on any `*/caddy.caddyfile` change) does
   this on its next run; by hand,
   `docker exec caddy caddy reload --config /etc/caddy/lab/caddy/Caddyfile`.
5. **The record**: `tofu-gatus.yaml` creates the CNAME on the push;
   `tofu-forgejo.yaml` creates `code.twolfe.dev` the same way.
   Or by hand from a laptop:
   `cd gatus/tofu && op run --env-file=secrets.env -- tofu init && op run --env-file=secrets.env -- tofu apply`
   — plan tripwire: 1 to add.
6. **Open the board** at https://status.twolfe.dev (fallbacks
   `https://gatus.lab.twolfe.dev`, `http://macmini.local:8280`) and work
   through the verification list.

## Upgrading

One pin, `image:` in `compose.yaml`. Bump via a normal PR; the push
deploys it. Read the release notes for
condition-syntax changes — every check is a condition string parsed at
load, and a changed parser is the one way a routine bump turns into the
invalid-config exit described under "Adding a check".
