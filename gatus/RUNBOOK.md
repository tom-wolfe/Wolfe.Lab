# Gatus runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap (one-time, in order)

1. **Mint the Forgejo token** ("The runners group" below) — the deploy
   resolves it and stays red until the vault item exists.
2. **Bring the container up**: the gatus workflow on the merge, or
   `scripts/deploy.sh gatus` from a checkout on the Pi.
3. **Re-issue the certificate** so it carries `status.twolfe.dev` (and
   `code.twolfe.dev`, added in the same change) — `caddy/README.md`
   "Neat names", step 4. At the desk. Until then the `.lab` and `.ts`
   names work and the neat name doesn't.
4. **Reload caddy's routes** — the caddy workflow (it fires on any `*/caddy.caddyfile` change) does
   this on its next run; by hand,
   `docker exec caddy caddy reload --config /etc/caddy/lab/caddy/Caddyfile`.
5. **The record**: `tofu-gatus.yaml` creates the CNAME on the push;
   `tofu-forgejo.yaml` creates `code.twolfe.dev` the same way.
   Or by hand from a laptop: `scripts/apply.sh gatus` — plan tripwire:
   1 to add.
6. **Open the board** at https://status.twolfe.dev (fallbacks
   `https://gatus.lab.twolfe.dev`, `http://macmini.local:8280`) and work
   through the verification list.

## The runners group

1. In Forgejo, as the admin user: Settings → Applications → Generate new
   token, name `gatus`, scope `read:repository` only. Store it as the
   1Password item `forgejo-gatus-token` (field `credential`) in the
   Wolfe.Lab vault.
2. Confirm the ids `config/runners.yaml` names are the live ones:

   ```sh
   curl -sH "Authorization: token $(op read op://Wolfe.Lab/forgejo-gatus-token/credential)" \
     http://macmini.local:3000/api/v1/repos/tom-wolfe/Wolfe.Lab/actions/runners?visible=true
   ```

   A runner re-registered with a NEW secret gets a new id; update the
   file. Re-running `forgejo/scripts/register-runner.sh` with the
   existing vault item keeps it.
3. Run the gatus workflow (or `scripts/deploy.sh gatus` on the Pi); the
   `runners` group is green within two minutes.

## Upgrading

One pin, `image:` in `compose.yaml`. Bump via a normal PR; the push
deploys it. Read the release notes for
condition-syntax changes — every check is a condition string parsed at
load, and a changed parser is the one way a routine bump turns into the
invalid-config exit described under "Adding a check".
