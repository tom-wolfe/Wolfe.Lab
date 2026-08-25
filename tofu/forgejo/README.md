# forgejo

Declaratively manages every repository on the Forgejo instance. State lives
in Garage (`s3://tofu-state/forgejo/terraform.tfstate`).

## The three repo shapes

| Shape | How | Meaning |
| --- | --- | --- |
| `mode = "mirror"` | pull mirror | Read-only copy, synced from GitHub every 8h |
| `mode = "active"` | one-time clone | Writable working fork; mirroring switched off |
| primary | `primary.tf` | Wolfe.Lab: Forgejo is the source of truth, push-mirrored to GitHub on every commit |

**Adding a repo** = one line in the `repos` map in `mirrors.tf`.

**Switching modes** = edit the `mode` value — but read this first:
Forgejo cannot convert between mirror and regular in place, so a mode flip
**replaces** the repository (tofu will plan a destroy + create):

- `mirror -> active`: safe — the mirror is destroyed and a fresh writable
  clone is taken from GitHub.
- `active -> mirror`: **destroys the active Forgejo copy, including any
  work not pushed elsewhere.** Push first, flip second. The plan output
  shows the replacement; treat `-/+ forgejo_repository` as a red flag to
  double-check.

## One-time setup

1. Create the 1Password items named in `secrets.env`:
   - `Forgejo API Token` — Forgejo -> Settings -> Applications -> Generate
     Token (read/write repository scope).
   - `GitHub PAT tom-wolfe`, `GitHub PAT nschema-org`,
     `GitHub PAT DisasterCare` — fine-grained, Contents: read-only, resource
     owner = that account/org, all (or selected private) repos. Orgs must
     allow fine-grained PATs (org Settings -> Personal access tokens).
   - `GitHub PAT Wolfe.Lab push` — fine-grained, Contents: read/write,
     scoped to tom-wolfe/Wolfe.Lab ONLY (this one can write; keep it narrow).

2. Delete the hand-made mirrors (they'd 409 against tofu's creates; mirrors
   are cattle — tofu recreates all of them uniformly):

   ```sh
   export FORGEJO_TOKEN=...   # or op read
   curl -s -H "Authorization: token $FORGEJO_TOKEN" \
     'http://macmini.local:3000/api/v1/users/tom-wolfe/repos?limit=50' \
     | jq -r '.[] | select(.mirror) | .name' \
     | xargs -I{} curl -s -X DELETE -H "Authorization: token $FORGEJO_TOKEN" \
         "http://macmini.local:3000/api/v1/repos/tom-wolfe/{}"
   ```

3. `op run --env-file=secrets.env -- tofu init`

## Day-to-day

```sh
op run --env-file=secrets.env -- tofu plan
op run --env-file=secrets.env -- tofu apply
```

State inspection: `op run --env-file=secrets.env -- tofu state list`.

## Notes

- The push mirror keeps the GitHub copy of Wolfe.Lab current on every
  commit, so everything that pulls from GitHub (the mini's chezmoi, the
  bootstrap one-liner, deploy keys) keeps working unchanged.
- Private mirrors store their PAT inside Forgejo per-repo, but Forgejo only
  consumes it at migration time — there is no API to update mirror
  credentials, so a `tofu apply` after rotating a PAT "succeeds" while the
  mirror keeps pulling with the dead token (2026-08-25). To rotate: update
  1Password, then set the new token in each repo's Settings -> Mirror
  Settings -> Authorization in the Forgejo UI, then run `tofu apply` once so
  state catches up (that apply is a harmless server-side no-op).
- No state locking (Garage lacks conditional writes): one operator, one
  machine at a time.

## Known provider issues (svalabs/forgejo 1.6.0)

- **Perpetual in-place "changes" on every repo** — the provider re-plans the
  computed `internal_tracker`/`permissions` blocks as unknown on every run
  (upstream #132, #169), and the resulting no-op PATCH can 500 on repos with
  wikis ("'' is not a valid branch name"). Suppressed with
  `lifecycle.ignore_changes` in `mirrors.tf`/`primary.tf`; plans are clean
  now. Remove the workaround once fixed upstream.
- **Deleting a repo outside tofu breaks refresh** ("Repository with ID N not
  found") — intentional per upstream #111. Recover with
  `tofu state rm 'forgejo_repository.repo["<name>"]'`, then apply to
  recreate.
- **`auth_token` updates are silent no-ops** — the provider accepts the
  change but Forgejo has no API for it (see the rotation note above). It
  should arguably be flagged RequiresReplace; not yet reported upstream.
- **Creates sometimes error with "invalid result object" and drop the new
  repo from state.** The repo IS created on Forgejo; adopt it instead of
  retrying:
  `tofu import 'forgejo_repository.repo["<name>"]' '<forgejo-owner>/<name>'`
- The same crash can leave a resource **tainted**; if the repo is healthy,
  `tofu untaint 'forgejo_repository.repo["<name>"]'` rather than letting it
  replace.

The "invalid result object" crash and the `auth_token` no-op are still worth
upstream issues at svalabs/terraform-provider-forgejo.
