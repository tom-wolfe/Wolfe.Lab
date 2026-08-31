# DNS

Zone-level records for `twolfe.dev` — the records that belong to the
*domain*, not to any service. Today that means the Proton Mail set (MX,
SPF, DKIM, DMARC, verification), created by hand during the mail
migration (2026-08) and imported here so the zone is recoverable from
this repo. Not a slice: no container, no flows, nothing deployed — one
tofu root and this file.

## The boundary: which records live where

The test is ownership, not record type. A record lives here if it would
still exist with the lab torn down — it exists because the *domain*
exists. A record that exists because a service exists lives with the
service:

- **The two front-door wildcards** (`*.lab`, `*.ts`) stay in
  `caddy/tofu` — they point at the front door, so the front door owns
  them (`caddy/tofu/records.tf` records the decision).
- **Per-service public names** (`git.twolfe.dev`) stay in the owning
  slice's tofu root — the contract in `caddy/README.md`, untouched.
- **Apex and `www`** are deliberately absent: they are Netlify's own
  site-attachment records (type `NETLIFY`), managed by the site, not
  declarable by the provider — its type list has no `NETLIFY` — and not
  worth declaring: detach and re-attach the site and they regenerate.

## On the name

`netlify/` would be wrong twice over: `caddy/README.md` already
establishes Netlify as a provider, not a slice; and the zone outlives
its host — move it to another DNS service and every record here
survives while a `netlify/` path lies. Nor is `dns/` a departure from
the technology-named directories elsewhere: those are named for the
thing they own (the `forgejo/` tree *is* Forgejo artifacts), and the
thing this root owns is the domain's records. Same convention, applied
to a thing that isn't a deployment.

## Imported, not recreated

These records were live and working before they were declared. Getting
an MX or SPF record subtly wrong doesn't error — it silently stops mail
or lands it in spam, and the feedback loop is days long. So the records
entered state via the `import` blocks in `tofu/imports.tf`, and the
gate is that the first plan shows **import only**:

```
Plan: 8 to import, 0 to add, 0 to change, 0 to destroy.
```

Anything else means a resource doesn't match reality — fix the
declaration, never let tofu "correct" a working mail record. Belt and
braces around the first apply:

```sh
dig +noall +answer MX twolfe.dev TXT twolfe.dev TXT _dmarc.twolfe.dev
```

before and after, and compare. `scripts/list-records.sh` prints every
record in the zone with its ID (the source of the import IDs), run as:

```sh
op run --env-file=tofu/secrets.env -- scripts/list-records.sh
```

Once the import has applied, `tofu/imports.tf` is dead weight — import
blocks are no-ops for records already in state — and can be deleted.

## Operational notes

- Plans and applies run from a workstation:
  `cd tofu && op run --env-file=secrets.env -- tofu plan`. Same
  1Password items as the other roots (`netlify-pat`,
  `tofu-state-passphrase`, garage state key) — nothing new to create.
- TTLs are declared at 300 because that is what the Proton setup
  created; declared explicitly so a hand-edit in the Netlify UI shows
  up as drift in the next plan rather than silently winning.
- The DKIM records are CNAMEs into `domains.proton.ch` by design:
  Proton rotates the actual keys on its side of the pointer, so
  declaring the CNAMEs here is safe and rotation needs no change.
- Changing any mail record starts at Proton's domain settings page
  (which knows the correct values), not here.
