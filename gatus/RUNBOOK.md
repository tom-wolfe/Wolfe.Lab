# Gatus runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap (one-time, in order)

1. **Bring the container up**: the gatus workflow on the merge, or
   `scripts/deploy.sh gatus` from a checkout on the Pi. No red-tick trap
   here — the `pushover` item it resolves already exists.
2. **Re-issue the certificate** so it carries `status.twolfe.dev` (and
   `code.twolfe.dev`, added in the same change) — `caddy/README.md`
   "Neat names", step 4. At the desk. Until then the `.lab` and `.ts`
   names work and the neat name doesn't.
3. **Reload caddy's routes** — the caddy workflow (it fires on any `*/caddy.caddyfile` change) does
   this on its next run; by hand,
   `docker exec caddy caddy reload --config /etc/caddy/lab/caddy/Caddyfile`.
4. **The record**: `tofu-gatus.yaml` creates the CNAME on the push;
   `tofu-forgejo.yaml` creates `code.twolfe.dev` the same way.
   Or by hand from a laptop: `scripts/apply.sh gatus` — plan tripwire:
   1 to add.
5. **Open the board** at https://status.twolfe.dev (fallbacks
   `https://gatus.lab.twolfe.dev`, `http://macmini.local:8280`) and work
   through the verification list.

## Upgrading

One pin, `image:` in `compose.yaml`. Bump via a normal PR; the push
deploys it. Read the release notes for
condition-syntax changes — every check is a condition string parsed at
load, and a changed parser is the one way a routine bump turns into the
invalid-config exit described under "Adding a check".
