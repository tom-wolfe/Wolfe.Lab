# Forgejo — home server

Self-hosted git server running on the Mac mini via Docker Compose.

| | |
|---|---|
| Web UI | http://macmini.local:3000 (also http://192.168.0.8:3000) |
| SSH clone port | 2222 |
| Image | `codeberg.org/forgejo/forgejo` pinned in `compose.yaml` |
| State | `~/Docker/forgejo/data` (bind mount → `/data` in the container) |
| Database | SQLite at `~/Docker/forgejo/data/gitea/forgejo.db` |
| Backups | `~/Docker/forgejo/backups` |

Config and state are deliberately split:

- **This directory** holds `compose.yaml`, `backup.sh` and this README. It is
  source-controlled and contains no secrets.
- **`~/Docker/forgejo/data`** holds everything stateful — repos, `app.ini`, the
  SQLite DB, SSH host private keys, JWT signing keys. It lives *outside* the
  git repo on purpose: those are secrets, and they'd otherwise be one
  `git add -A` away from being committed. Set `FORGEJO_DATA` to relocate it
  (both `compose.yaml` and `backup.sh` honour it).

Nothing is stored in the container itself, so the image can be replaced freely.

## Day-to-day

```sh
cd ~/Docker/forgejo

docker compose ps            # status
docker compose logs -f       # follow logs
docker compose restart       # restart
docker compose down          # stop (data is untouched)
docker compose up -d         # start
```

`restart: unless-stopped` brings the container back automatically — **but only
once the Docker daemon is running.** See "After a reboot" below.

## First-run setup

On a fresh data directory, http://macmini.local:3000 shows the installer with
the database and URL fields pre-filled from `compose.yaml`. Leave them alone and
submit — the admin fields at the bottom are optional, and the **first account to
register becomes an admin** either way.

> **Never put `INSTALL_LOCK` in `compose.yaml`.** It is one-time state written
> by the installer, not configuration. Because `environment-to-ini` re-applies
> every `FORGEJO__*` variable on each start, pinning it there reverts what the
> installer just saved, and Forgejo aborts fatally on the next boot. See
> Troubleshooting.

Then lock it down, since this is a LAN server that doesn't need public signups:

1. Register your account at http://macmini.local:3000/user/sign_up
2. Set `FORGEJO__service__DISABLE_REGISTRATION: "true"` in `compose.yaml`
   (this one *is* safe to manage via env — it's ordinary config, and setting it
   is idempotent)
3. `docker compose up -d`

Set `FORGEJO__service__REQUIRE_SIGNIN_VIEW: "true"` as well if you don't want
repos browsable by anyone on the LAN without logging in.

## Cloning

HTTP:

```sh
git clone http://macmini.local:3000/<user>/<repo>.git
```

SSH — add your public key in the web UI under *Settings → SSH / GPG Keys*, then:

```sh
git clone ssh://git@macmini.local:2222/<user>/<repo>.git
```

The non-standard port is easy to forget. Put this in `~/.ssh/config` and plain
`git@macmini.local:user/repo.git` works:

```
Host macmini.local
    Port 2222
    User git
```

## Upgrading

State lives in `data/`, so upgrades are a tag bump. **Back up first** — an
upgrade runs irreversible database migrations, and rolling back to an older
image after that will fail.

```sh
cd ~/Docker/forgejo
./backup.sh                      # snapshot before touching anything
$EDITOR compose.yaml             # bump the image tag
docker compose pull
docker compose up -d
docker compose logs -f           # watch migrations complete
```

Rules worth respecting:

- **Never skip a major version.** To go 16 → 18, stop at 17 first, let it start
  and finish migrating, then move on.
- Read the release notes for major bumps: https://codeberg.org/forgejo/forgejo/releases
- If it won't start after an upgrade, restore the backup (below) and pin the
  old tag again.

Checking what's current:

```sh
curl -s "https://codeberg.org/api/v1/repos/forgejo/forgejo/releases?limit=5" \
  | grep -o '"tag_name":"[^"]*"'
```

## Backup

```sh
./backup.sh
```

Writes a timestamped tarball to `~/Docker/forgejo/backups/` and keeps the last
10. It stops the container first — a live SQLite file copied mid-write can be
inconsistent — and starts it again afterwards, so expect ~30s of downtime.

Because `data/` is an ordinary folder, Time Machine already covers it too, but
only the cold-copy caveat above makes those snapshots trustworthy; prefer
`backup.sh` before anything risky.

### Restore

```sh
cd ~/Development/Wolfe.Tools/forgejo
docker compose down
rm -rf ~/Docker/forgejo/data                   # or move it aside first
tar -xzf ~/Docker/forgejo/backups/forgejo-YYYYmmdd-HHMMSS.tar.gz \
    -C ~/Docker/forgejo
docker compose up -d
```

Make sure the image tag in `compose.yaml` matches the version the backup was
taken with, or Forgejo may refuse to start against an older schema.

## After a reboot

Docker Desktop is **not** currently set to launch at login, so nothing starts
itself after a restart. Fix it once:

*Docker Desktop → Settings → General → tick "Start Docker Desktop when you sign
in" → Apply & restart.*

Until that's on, after every reboot you need:

```sh
open -a Docker && cd ~/Docker/forgejo && docker compose up -d
```

Note this is tied to **signing in**, not to boot — a Mac mini sitting at the
login screen after a power cut won't run Forgejo. If that matters, enable
automatic login in System Settings → Users & Groups.

## Troubleshooting

**Clicking Install gives `ERR_CONNECTION_RESET` and returns you to the install
page.** Caused by `FORGEJO__security__INSTALL_LOCK` being set in `compose.yaml`.
The sequence:

1. Install writes `INSTALL_LOCK = true` to `app.ini`, then restarts
2. The image's `environment-to-ini` (`/etc/s6/gitea/setup`) re-stamps every
   `FORGEJO__*` variable over `app.ini`, reverting it to `false`
3. Forgejo sees an installed database with an unlocked install and dies:
   `MustInstalled() [F] Unable to load config file for a installed Forgejo
   instance` — the process drops mid-request, hence the connection reset
4. It restarts onto the install page, so the attempt looks like a no-op

Fix: remove `INSTALL_LOCK` from `compose.yaml` entirely and let `app.ini` own it.
The general rule — anything the installer or the app writes to `app.ini` as
*state* must not be pinned via `FORGEJO__*`, because those variables win on
every boot.

```sh
docker exec forgejo grep -i INSTALL_LOCK /data/gitea/conf/app.ini   # want: true
docker compose logs | grep MustInstalled
```

**Container restart-loops with `bind: address already in use` on port 22.**
The image runs its own OpenSSH daemon as an s6 service (`/etc/s6/openssh/run`),
which always binds container port 22 and can't be disabled by configuration.
Setting `FORGEJO__server__START_SSH_SERVER: "true"` starts a *second* SSH server
on that same port; whichever loses the race dies, and when it's Forgejo's the
whole container exits. Fix: don't set `START_SSH_SERVER` at all. `SSH_PORT` only
controls the port advertised in clone URLs.

Note that a passing healthcheck does **not** rule this out — the check only
probes HTTP, so read the logs when diagnosing:

```sh
docker compose logs | grep -iE 'address already in use|\[F\]|fatal'
docker inspect forgejo --format '{{.RestartCount}}'   # >0 and climbing = looping
```

## Notes

- Config is set through `FORGEJO__section__KEY` environment variables rather
  than by hand-editing `data/gitea/conf/app.ini`. Those variables are written
  into `app.ini` on every container start, so `compose.yaml` stays the single
  source of truth and edits made directly to `app.ini` get overwritten.
- `ROOT_URL` must match the address you actually type in the browser. If you
  later add a domain or reverse proxy, update `FORGEJO__server__DOMAIN` and
  `FORGEJO__server__ROOT_URL` together, or generated clone links and redirects
  will point at the wrong host.
- Nothing here is exposed to the internet. The ports are published on all
  interfaces, so anything on the LAN can reach it, but no router port
  forwarding is configured.
