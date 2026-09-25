# Renovate — runbook

## Bring-up

1. **Two vault items, before the pull request** — the tofu checks plan
   with them:
   - `forgejo-renovate`, a password item: the `renovate` user's password.
     Generate a long one; nobody types it.
   - `github-renovate-pat`: a fine-grained GitHub token, public
     repositories only, no permissions — read-only lookups, nothing more.
2. **Merge.** `renovate tofu / check` fails on the first pull request —
   it logs in as a user that does not exist yet — and is not yet required,
   so merge anyway. `forgejo tofu` creates the user and gives it write
   access; `renovate tofu` mints its token and writes both Actions secrets.
   Both queue on the mini; if `renovate tofu` happens to run first, it
   fails the same way, and re-running it after `forgejo tofu` is the fix.
   The same merge makes `renovate / check` and `renovate tofu / check`
   required statuses.
3. **Run it once by hand**: Actions → `renovate` → Run workflow. It opens
   the dependency dashboard issue and the first pull requests, a couple
   an hour (Renovate's default limit) until it has caught up.

## Rotating a token

- **Renovate's Forgejo token**:
  `tofu apply -replace=forgejo_personal_access_token.renovate` from
  `renovate/tofu`, with its secrets resolved as for any root
  (`forgejo/README.md`, "Day-to-day"). The secret follows it in the same
  apply.
- **The GitHub token**: update `github-renovate-pat` in the vault, then
  re-run the `renovate tofu` workflow.
- **The password**: update `forgejo-renovate` in the vault, then re-run
  `forgejo tofu` (which sets it on the user) and `renovate tofu` after it.

## When it misbehaves

- **Why didn't it propose X?** The dependency dashboard lists what it
  found, what it is holding back, and every lookup that failed. The run's
  log (`LOG_LEVEL: info` in the workflow; `debug` for one run) has the
  rest.
- **Checking a config change before merging it**: install the same
  version into a scratch path (`npm install renovate@<version>`) and run,
  from the repo root,
  `RENOVATE_REPOSITORIES='[]' RENOVATE_CONFIG_FILE=renovate/config.json5 renovate --platform=local --dry-run=lookup`.
  It reads the working copy and prints every dependency and the update it
  would propose, without touching Forgejo.
