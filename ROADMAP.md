# Roadmap

What's coming and *why*, in rough order. `CHANGELOG.md` is the record of
what happened; this is the record of what we decided to do next and what we
decided to leave alone. Items move out of here into the changelog when they
ship.

Ordering principle: reduce risk before adding surface. Anything that makes
a failure visible outranks anything that adds a new thing to fail.

## Next

### 1. Tailscale — a `tailscale/` slice

Host-level client on all three Macs via the Brewfile first; MagicDNS on; no
subnet router. Non-invasive, nothing existing changes. It's early in the
list because three things already written down depend on it:

- the neat public names (`jellyfin.twolfe.dev`) point at a Tailscale IP —
  see the edge/DNS design;
- the portless Forgejo clone URL waits on a dedicated IP with a free port
  22 (`forgejo/compose.yaml` records that decision);
- `100.64/10` isn't touched by the router's DNS-rebind protection, so the
  DHCP-advertised-resolver workaround in `caddy/README.md` can come out.

Per-service sidecars come later, when the Forgejo port-22 case is worth it.
Accepted trade-off: Tailscale's coordination server is a cloud dependency.
Headscale is rejected — if the lab is down you can't reach the lab to fix
it.

### 2. Offsite backup — restic to Backblaze B2

Native restic encryption (repo password from 1Password, same
vault-is-the-origin pattern as everything else). Not only offsite: the
current scheme keeps **ten full tarballs per service**, and restic's
content-addressed dedup plus compression collapses those to roughly one
snapshot plus deltas. It fixes the space problem and the no-second-copy
problem in one move. `garage/rclone.env` already proves the S3 plumbing.

### 3. Obsidian vaults into git

Replaces Google Drive as the vaults' storage with an hourly commit-and-push
job to Forgejo. Better on every axis: history, dedup, rides the existing
`lab.forgejo/backup`, and it drops the `~/Library/CloudStorage` dependency
that's the reason sshd needs "Full Disk Access for remote users" granted.

### 4. Renovate as a Kestra flow

Roughly nine pinned images across the slices (kestra, postgres, caddy,
forgejo, jellyfin, garage, lego, beszel). Renovate runs fine as a container
task — it does **not** need the Actions runner — and it automates the "bump
the pin via a normal PR first" ritual `kestra/README.md` currently asks for
by hand.

### 5. Forgejo Actions runner

The big structural unlock, and still the plan of record from phase 3:
CI, the changelog check (Forgejo issue #10), plan-on-PR and apply-on-merge.
It also closes a real correctness gap — Garage has no state locking, so
concurrent `tofu apply`s are currently prevented by operator discipline
alone, and the runner becomes the serialization point.

## Undecided

### General file sharing (the third thing Google Drive does)

Backup and vault storage have answers above; sharing files with *other
people* doesn't. It's the only item here that would require exposing
something to the internet, which the lab has never done. Options, unranked:
Tailscale node-sharing if "a few known people" covers it; a public
Nextcloud/Seafile if it doesn't, which deserves its own design pass; or
keep a SaaS for the small subset actually shared and self-host the rest.
Explicitly not blocking the backup work — they're independent.

### New services: Plane, OpenGist, Immich

Wanted, but each adds backup surface. Immich in particular is large and is
the one where data loss actually hurts — it should land *after* offsite
backup exists, not before.

## Deliberately deferred

### Self-hosted secrets (1Password Connect / OpenBao)

The original goal was cutting the cloud dependency. The `create_` template
pattern already achieves the operative part: `op` is a bootstrap
dependency, not a tick dependency, and rotation is delete-the-file-and-
apply. OpenBao would add an unseal ritual and a genuine bootstrap
circularity — lab down, can't reach secrets, can't bring lab up. Small
remaining gain, real added fragility. Revisit if the calculus changes.

### Kubernetes + Argo CD (`k8s/`)

Kept as a learning goal, not as a solution to a current problem. The lab
has a working pull-based CD loop and a clean security model — no
*container* gets the Docker socket, one audited SSH door — and Kubernetes on
a single mini via Docker Desktop is a lot of machinery for one node that
would dissolve the vertical-slice model into manifests. Worth doing if the
point is to learn it; worth being honest that it isn't fixing anything.
