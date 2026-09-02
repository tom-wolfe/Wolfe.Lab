# Gatus

The status page: a green or red light per service, decided by a
**request**, not by whether a container is running. [Gatus](https://gatus.io)
sends each check on an interval, evaluates conditions against what came
back (status, body, certificate expiry, connection), draws the board at
https://status.twolfe.dev and pushes to Pushover when something turns
red.

Endpoint checks were the one shape of failure nothing here saw. Beszel is
agent-based — it reports that a host is loaded or a container stopped. It
cannot tell you a container is *up and wedged*, and that gap is not
hypothetical: caddy's healthcheck sat red for 33 hours while caddy served
traffic perfectly, and nothing noticed, because nothing asked.

| Concern | Handled by |
| --- | --- |
| Container | the `lab.gatus/deploy` flow, chained on the chezmoi tick like every stack; first bring-up via `setup.sh` |
| **The checks** (`config/*.yaml`) | **this repo.** Bound read-only into the container; Gatus reloads on change, so a merged edit is live on the next tick without a deploy |
| Pushover credentials (`~/Docker/gatus/gatus.env`) | chezmoi `create_` template (`chezmoi/home/Docker/gatus/`), from the existing `pushover` vault item |
| History (`~/Docker/gatus/data`) | disposable — **no backup flow**, see "Nothing to back up" |
| Gatus's own liveness | the `lab.gatus/health` flow — a status page cannot show itself being down |
| Route (`gatus.lab.twolfe.dev`, `gatus.ts.twolfe.dev`, `status.twolfe.dev`) | `caddy.caddyfile`, imported by the front door |
| The `status.twolfe.dev` record | `tofu/` — a root born for one CNAME, applied by `lab.gatus/apply` on push, drift-checked by `lab.gatus/plan` daily |

## Why Gatus and not Uptime Kuma

The roadmap said Kuma. Decision 2026-09-02 (Tom's), on two grounds:

1. **The checks are code.** Kuma's configuration is UI-only — its write
   API is Socket.IO with no REST and no OpenTofu provider — and the
   roadmap had already named that as the cost: a second slice after
   beszel whose config isn't declarable. Gatus is a YAML file. That is
   the whole repo's shape, and with a few dozen checks it is also the
   difference between a merge and an afternoon of clicking.
2. **Config-as-code dissolved the reason to wait for the Pi.** The
   earlier call was "don't land on the mini as a stopgap", because the
   cost of a stopgap is migrating UI-entered config later. With the
   config in git, moving is a deploy-target change — so Gatus runs on the
   mini today and moves when the Pi joins the fleet (roadmap item 1).

What Kuma has that Gatus doesn't: more monitor types, and a public status
page with incident posts. Neither matters for a private board.

## How the checks are organised

One file per group under `config/`; `GATUS_CONFIG_PATH` names the
directory and Gatus deep-merges every file in it (maps merge, endpoint
lists append — scalars may only be set once, and `gatus.yaml` is the
only file that sets any).

| File | Group | Asks |
| --- | --- | --- |
| `lab.yaml` | `lab` | each service **directly**, by container name over the `lab` network: is the app up and answering? |
| `front-door.yaml` | `front door` | the same services **as a browser would** — public name, TLS, through caddy. One request exercises Netlify DNS, the wildcard cert, caddy's routing and the upstream |
| `dependencies.yaml` | `dependencies` | the third parties the lab stands on: 1Password, Backblaze, GitHub, healthchecks.io, Pushover, Netlify DNS, Tailscale |
| `personal.yaml` | `personal` | services cared about that the lab doesn't depend on: Proton Mail |
| `gatus.yaml` | — | UI, storage, alerting — everything that isn't a check |

**Direct and front door together localise a fault.** Lab green, front
door red: the edge — DNS, certificate, caddy. Both red: the service. Front
door green alone would be enough for a status light; the split is what
makes the light useful at 2am.

**Third parties get two kinds of check where both are possible.** The
vendor's status page, read as JSON — Atlassian Statuspage exposes
`/api/v2/status.json` with `status.indicator == none` when all is well,
and GitHub, 1Password, Proton, Tailscale and Netlify all use it. And a
direct probe of the thing the lab actually talks to, because a status
page reports what the vendor *admits*, late: the 1Password rate-limit
outage of 2026-09-01 never appeared on any status page. Backblaze is the
odd one out (FireHydrant, not Statuspage) — its check is marked
unverified in the file, for reasons the comment there explains.

**Alert priorities follow the lab's `alert: high/low` idea.** Lab and
front-door failures push at Pushover priority 0; third parties at -1
(quiet) — things to know, not things anyone here can fix. Recoveries are
always quiet. Three consecutive failures before any push, so at the
2-minute lab interval a service is ~6 minutes down before the phone
buzzes: the flap control this lab has wanted since 0.10.0.

## Adding a check

Add an endpoint to the right file, merge, wait for the tick. The tick's
`git pull` rewrites the bound `config/` directory in place and Gatus
reloads it — **no deploy, no container recreate.** Adding a whole group
is a new file in the same directory.

Two consequences of that convenience, both loud rather than silent:

- **An invalid config makes Gatus exit** (upstream's default, and the
  right one — the alternative is running on stale config while looking
  healthy). Docker restarts it, it exits again, and `lab.gatus/health`
  goes red within fifteen minutes with `alert: high`. So a bad merge to
  `config/` is a paged incident, not a quiet one. Check YAML before
  merging; `yq . gatus/config/*.yaml` from the repo root is the cheap
  syntax pass, and `docker compose --project-directory gatus config`
  validates the compose side.
- **Environment changes do NOT reload** — only files do. Rotating the
  Pushover credentials means recreating the container (below).

Conditions worth knowing, all used in this slice: `[STATUS] == 200`,
`[BODY].status.indicator == none` (JSONPath), `[BODY] == Healthy` (plain
text), `has([BODY].incidents) == false`, `[CONNECTED] == true` for
`tcp://` / `tls://` / `starttls://`, `[CERTIFICATE_EXPIRATION] > 336h`,
and `dns:` endpoints with `[DNS_RCODE] == NOERROR`. Upstream's README has
the full table.

## Who watches the watcher

`lab.gatus/health` — a Kestra flow polling Gatus's own `/health` at
11/26/41/56 past the hour, `alert: high`. Gatus's Pushover alerts cannot
report Gatus being down, and `lab.gatus/deploy` is a convergent no-op
that stays green regardless, so without this a dead status page looks
exactly like a page you haven't opened.

Gatus watches Kestra back (`config/lab.yaml` polls the management
`/health` every two minutes), which makes a cycle — and a cycle is not an
outside observer. Both share the mini's fate. That is why neither is the
dead man's switch: healthchecks.io is, and it stays outside the building
(`chezmoi/tofu/`). The layer table in the root README has the full
picture.

## Secrets

No new vault item. The Pushover application token and user key are the
same `pushover` item `system/alert-failed` and Beszel use, so every alert
in the lab lands in one place. The `create_` template writes them to
`~/Docker/gatus/gatus.env`; compose passes the file as `env_file`; the
config references `${PUSHOVER_APP_TOKEN}` and `${PUSHOVER_USER_KEY}`,
which Gatus substitutes from its environment at load. The repo carries no
secret material.

Rotation: update the vault item, `rm ~/Docker/gatus/gatus.env`, `chezmoi
apply` on the mini, then `docker compose --project-directory
~/.local/share/chezmoi/gatus up -d --force-recreate` — an env change is
not a file change, so Gatus's reload doesn't see it.

## The neat name

`status.twolfe.dev` is the `git.twolfe.dev` pattern for a web route: a
CNAME to `gatus.ts.twolfe.dev` in `tofu/records.tf`, so it resolves to
wherever caddy's `*.ts` wildcard points — the mini's tailnet address —
and this root never holds an IP. Tailnet path on purpose: the people who
want a status page carry the tailnet. The two wildcard names still work
and are what the lab's own checks and flows use (`gatus:8080` on the
Docker network, never a public name — names are for humans).

An apex-level name matches no wildcard, so the front door needs it in
two places — the site address and the certificate's SAN list — and the
certificate has to be re-issued once. The full recipe, including the
re-issue ritual, is `caddy/README.md` "Neat names"; the record itself is
the smallest tofu root in the repo and the template for the next one.

## Nothing to back up

`~/Docker/gatus/data/gatus.db` holds check history: the uptime bars and
response-time graphs. The *configuration* is `config/` and lives in git,
so this is the first stateful slice with no backup flow — deliberately.
Losing the file costs a week of green bars and nothing else, and adding a
nightly stop/snapshot/start for that would be backup surface for its own
sake. If Gatus ever grows state that isn't reproducible from the repo,
that decision reverses.

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
4. **Reload caddy's routes** — the tick-chained `lab.caddy/deploy` does
   this on its next run; by hand,
   `docker exec caddy caddy reload --config /etc/caddy/Caddyfile`.
5. **Register the flows**: `cd kestra/tofu` and
   `op run --env-file=secrets.env -- tofu apply` — plan tripwire: 4 to
   add (`lab.gatus/deploy`, `health`, `plan`, `apply`), 0 to change or
   destroy. `lab.gatus/apply` then creates the CNAME on the next push
   to main; `lab.forgejo/apply` creates `code.twolfe.dev` the same way.
   Or by hand from a laptop:
   `cd gatus/tofu && op run --env-file=secrets.env -- tofu init && op run --env-file=secrets.env -- tofu apply`
   — plan tripwire: 1 to add.
6. **Open the board** at https://status.twolfe.dev (fallbacks
   `https://gatus.lab.twolfe.dev`, `http://macmini.local:8280`) and work
   through the verification list.

## Verify on first deploy

The whole config was run from a laptop on the LAN before merging
(2026-09-02: the pinned image, this `config/`, dummy Pushover keys): all
29 endpoints parsed, every vendor check passed as written — Backblaze's
payload condition on an all-clear, Proton's port-25 STARTTLS from the
house's line, `git.twolfe.dev:22` from inside a Docker Desktop VM — and
every front-door check passed with its certificate condition. So the
conditions are right. What a laptop cannot prove is the mini's own
vantage point. Three checks still carry a `VERIFY` note in their file;
give them ten minutes to settle, then:

- **`front door` group, all of it.** From inside the mini's Docker
  Desktop VM the `*.lab` names resolve to the mini's OWN LAN address and
  loop back through the host's published `:443`. If the whole group is
  red while the browser is fine, that loop doesn't work — the fix is a
  `client.dns-resolver` or an explicit address, and it goes in the file
  with a note. (`code.twolfe.dev` was red on the laptop only because the
  record didn't exist yet.)
- **`lab/kestra`** — Micronaut's management port `:8081` must bind beyond
  loopback for a sibling container to reach it. Red while Kestra is fine
  means it doesn't; fall back to the UI root and note it.
- **`dependencies/git.twolfe.dev`** — asks the mini's VM to route to the
  tailnet. It worked from a laptop's VM, which is strong evidence; if
  it's red on the mini while `git fetch` works from a laptop, delete the
  check.

And two claims about vendors that only an incident can verify:
Backblaze's payload shape during an outage (the comment in
`dependencies.yaml`), and that the `.com` 1Password region is the
account's.

A known-false red must not stay on the board. A status page anyone has
learned to ignore is worse than none.

## Moving to the Pi

Roadmap item 1. Known now because the config is code:

- `front-door.yaml`, `dependencies.yaml`, `personal.yaml` — **unchanged.**
  Public names and third parties look the same from anywhere.
- `lab.yaml` — **every URL changes.** Container names resolve only on the
  mini's `lab` Docker network; from the Pi each becomes the
  `macmini.local:<port>` address in `ENDPOINTS.md` (Kestra's `:8081` is
  unpublished — that check becomes the UI root on `:8180`).
- `compose.yaml` — the `lab` external network goes (nothing to join on
  the Pi); the caddy route stays on the mini and points at the Pi's
  address instead of a container name. `status.twolfe.dev` is a CNAME
  to the front door, so the record does not move — only the snippet's
  upstream does.
- `lab.gatus/deploy` — a second `lab-job` bridge target, which is the
  fleet-model expansion the smart-home roadmap item costs out.
- `lab.gatus/health` — `gatus:8080` becomes the Pi's address.

Gained by the move: Gatus becomes external to the mini and catches
"the mini is down" in two minutes rather than healthchecks.io's ten.

## Upgrading

One pin, `image:` in `compose.yaml`. Bump via a normal PR; the tick ships
it and `lab.gatus/deploy` converges it. Read the release notes for
condition-syntax changes — every check is a condition string parsed at
load, and a changed parser is the one way a routine bump turns into the
invalid-config exit described under "Adding a check".

## Operational notes

- Logs: `docker logs gatus`. Reload events and "configuration file was
  updated, but it is not valid" both appear there.
- Liveness by hand: `curl -s http://macmini.local:8280/health` →
  `{"status":"UP"}`.
- Read-only API: `/api/v1/endpoints/statuses` (all), or
  `/api/v1/endpoints/<group>_<name>/statuses` with spaces in either part
  replaced by `-`. Handy for a future Kestra flow that wants "is X green"
  without parsing a dashboard.
- Concurrency is upstream's default of 3 checks at a time. Fine at this
  size; if a slow vendor ever holds the lab checks back, raise
  `concurrency` in `gatus.yaml`.
- The `:8280` publish is the fallback for when the front door is down —
  for a status page that is precisely the moment it's wanted. Don't tidy
  it away.
