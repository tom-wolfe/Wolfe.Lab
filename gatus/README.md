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
| Container | **on the Pi** — `.forgejo/workflows/gatus.yaml`, on every push that touches this slice; `scripts/deploy.sh gatus` on the node itself |
| **The checks** (`config/*.yaml`) | **this repo.** Bound read-only into the container; Gatus reloads on change, so a merged edit is live on the next tick without a deploy |
| Pushover credentials | `secrets.env` names the existing `pushover` vault item; the deploy resolves both fields into the container's environment |
| History (`~/Docker/gatus/data`) | disposable — **no backup flow**, see "Nothing to back up" |
| Gatus's own liveness | `.forgejo/workflows/gatus-health.yaml`, from the mini — a status page cannot show itself being down, and the watcher is on the other machine |
| Route (`gatus.lab.twolfe.dev`, `gatus.ts.twolfe.dev`, `status.twolfe.dev`) | `caddy.caddyfile`, imported by the front door on the mini; upstream is the Pi's address |
| The `status.twolfe.dev` record | `tofu/` — a root born for one CNAME, applied by `.forgejo/workflows/tofu-gatus.yaml` on push, drift-checked by the same workflow daily |

## Why Gatus and not Uptime Kuma

Uptime Kuma was the original plan. Gatus won on two grounds:

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
| `runners.yaml` | `runners` | each Actions runner's status, asked of Forgejo's API: is the thing that runs every deploy, backup and alert still polling? |
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
outage never appeared on any status page. Backblaze is the
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
  healthy). Docker restarts it, it exits again, and `gatus-health.yaml`
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

`.forgejo/workflows/gatus-health.yaml` — the mini's runner polls Gatus's
`/health` on the Pi at 11/26/41/56 past the hour and pages if it isn't
`UP`. Gatus's Pushover alerts cannot report Gatus being down, and the
deploy workflow is a convergent no-op that stays green regardless, so
without this a dead status page looks exactly like a page you haven't
opened.

Gatus watches the mini back (`config/lab.yaml`, every two minutes): two
machines watching each other rather than one watching itself. The mini
down turns the whole `lab` group red in two minutes; the Pi down turns
`gatus-health` red within fifteen. A runner that has stopped polling
(`config/runners.yaml`) turns its own light red in six: Forgejo reports
a runner `offline` a minute after its last poll, and a dead runner is
otherwise silence — its scheduled workflows simply stop being run, and
nothing inside the lab noticed until the dead man's switch fired. Neither is the dead man's switch —
healthchecks.io is, and it stays outside the building (`chezmoi/tofu/`).

## Secrets

The Pushover application token and user key are the same `pushover`
item `scripts/alert.sh` and Beszel use, so every alert in the lab lands
in one place. `forgejo-gatus-token` is the one item of Gatus's own: a
Forgejo token scoped `read:repository`, because the runners route wants
a login and the `forgejo-api-token` the tofu holds can write. Minted by
hand (`RUNBOOK.md` "The runners group"). `secrets.env` names the three;
the deploy resolves them into Gatus's environment, and the config
references `${PUSHOVER_APP_TOKEN}`, `${PUSHOVER_USER_KEY}` and
`${FORGEJO_GATUS_TOKEN}`, which Gatus substitutes at load. Neither the
repo nor the node's disk carries the values.

Rotation: update the vault item and re-run the gatus workflow — a changed
environment is a changed container, so compose recreates it.

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

## Placement

It lives on the Pi, and everything the move changed is in files:

- `front-door.yaml`, `dependencies.yaml`, `personal.yaml` — unchanged.
- `lab.yaml` — every URL is now `${LAB_HOST}:<published port>`, the
  mini's MagicDNS name set once in `compose.yaml` — no address in the repo;
  Tailscale resolves and routes it from a Pi container (verified), the same
  choice the Beszel agent made for its hub URL. One check went:
  "forgejo ssh" (the container's `:22` is not on the LAN;
  `dependencies.yaml` already checks it by its tailnet route).
  One check is new: `mini`, sshd on the host — when it and everything
  below it is red, it is the machine, which is the whole point of the move.
- `compose.yaml` — no `lab` network; paths under `${HOME}` (the runner's
  job environment carries the login user's `HOME`). The project directory
  is the slice's install, `~/.local/share/Wolfe.Lab/gatus`, which
  `scripts/deploy.sh` refreshes from the checkout on every deploy.
- `caddy.caddyfile` — upstream is the Pi's MagicDNS name; Docker Desktop's
  resolver follows macOS's, so the caddy container resolves it (verified).
- `.forgejo/workflows/gatus.yaml` deploys on push; `gatus-health.yaml`
  probes the Pi by its MagicDNS name.

## Operational notes

- Logs: `docker logs gatus` on the Pi. Reload events and "configuration file was
  updated, but it is not valid" both appear there.
- Liveness by hand: `curl -s http://wolfe-pi5.tailf823b8.ts.net:8280/health` →
  `{"status":"UP"}`.
- Read-only API: `/api/v1/endpoints/statuses` (all), or
  `/api/v1/endpoints/<group>_<name>/statuses` with spaces in either part
  replaced by `-`. Handy for a future workflow that wants "is X green"
  without parsing a dashboard.
- Concurrency is upstream's default of 3 checks at a time. Fine at this
  size; if a slow vendor ever holds the lab checks back, raise
  `concurrency` in `gatus.yaml`.
- The `:8280` publish is the fallback for when the front door is down —
  for a status page that is precisely the moment it's wanted. Don't tidy
  it away.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
