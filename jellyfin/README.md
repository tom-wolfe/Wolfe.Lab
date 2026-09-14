# Jellyfin — home server

Migrated from the native macOS `Jellyfin.app` to Docker Compose on the Mac mini,
keeping the existing library, watch history and artwork exactly as they were.

| | |
|---|---|
| Web UI | http://macmini.local:8096 (also http://192.168.0.7:8096 or http://192.168.0.8:8096 — the mini has two interfaces) |
| Image | `jellyfin/jellyfin` pinned in `compose.yaml` |
| State | `~/Library/Application Support/jellyfin` (bind mounted at the **same path** inside the container) |
| Database | SQLite at `~/Library/Application Support/jellyfin/data/jellyfin.db` |
| Media | `/Volumes/Data1` and `/Volumes/Data2` (both USB, mounted at the same paths) |
| Backups | restic repo on `/Volumes/Data2` + B2 offsite (nightly; `restic/README.md`) |

## Deployment

`.forgejo/workflows/jellyfin.yaml` converges this stack on every push that touches it, on
the mini's host runner. Manual converge: run the workflow from the Actions
tab, or on the mini:

```sh
scripts/deploy.sh jellyfin /Volumes/Data1 /Volumes/Data2
```

## Where account passwords live

In `jellyfin.db`, not in any env file — recreating the container never
touches them, and restoring a backup restores them. Keep the canonical copy
in 1Password; only a from-scratch data reset (setup wizard) ever asks for a
password again, and then you re-enter the vaulted one — Jellyfin has no CLI
to set it for you.

## Why the paths look strange

This is the one thing to understand before editing anything.

Jellyfin stores **absolute** paths in its database. In this install that means:

- the 7 library roots are rows pointing at
  `/Users/tomwolfe/Library/Application Support/jellyfin/root/default/<Name>`
- ~2,700 artwork rows point at `<datadir>/metadata/People/...`
- all 25,285 items point at `/Volumes/Data1/...` or `/Volumes/Data2/...`
- item IDs are *derived from* the path, and watch history hangs off item IDs

The conventional Docker layout — bind the data directory to `/config` and the
media to `/media` — invalidates every one of those rows. Jellyfin would come up,
find nothing where the database says it should be, and rebuild the library from
scratch. The library would look superficially fine and every "watched" mark,
favourite and collection would be gone.

So this setup does the opposite of remapping: it reproduces the host paths
verbatim inside the container and points Jellyfin's directory environment
variables at them.

```yaml
JELLYFIN_DATA_DIR: "/Users/tomwolfe/Library/Application Support/jellyfin"
```
```yaml
- type: bind
  source: "/Users/tomwolfe/Library/Application Support/jellyfin"
  target: "/Users/tomwolfe/Library/Application Support/jellyfin"
```

Not one byte of the database was rewritten during the migration. The trade-off
is that this compose file is tied to this machine and this user — it is not
portable to a Linux box as-is. That was the right trade for an install with
years of watch history in it.

**Do not "tidy" the paths.** Changing a volume target or a `JELLYFIN_*_DIR`
value breaks the library. If you ever genuinely need to relocate, the work is a
database migration, not a config edit.

This is also why the state does **not** live in `~/Docker/jellyfin/` the way
Forgejo's does — it has to stay where the native app left it.

## Day-to-day

```sh
cd ~/.local/share/Wolfe.Lab/jellyfin

docker compose ps            # status
docker compose logs -f       # follow logs
docker compose restart       # restart
docker compose down          # stop (data is untouched)
docker compose up -d         # start
```

## Known limitations of running this in Docker on macOS

Worth knowing before you hit them:

- **No hardware transcoding, ever.** VideoToolbox is not reachable from a Linux
  container on macOS. Your config had `HardwareAccelerationType: none` already,
  so nothing regressed — but the option is now permanently off the table.
  Transcodes are CPU-only. Direct play is unaffected, which is the common case.
- **Client auto-discovery no longer works.** Jellyfin apps find servers by UDP
  broadcast on port 7359; those packets don't cross Docker Desktop's NAT on
  macOS. Clients need the address typed in once: `http://macmini.local:8096`.
- **The USB drives must be mounted before the container starts.** If `Data1` or
  `Data2` is missing at start, the bind mount resolves to an empty directory and
  those libraries look empty. Nothing is deleted — Jellyfin aborts a scan that
  finds a library root entirely missing — but restart the container once the
  drives are back.
- **Docker Desktop must be running.** `restart: unless-stopped` only applies once
  the daemon is up. See "After a reboot".

## Notes

- The image ships its own healthcheck (`curl` against `$HEALTHCHECK_URL`), so
  `compose.yaml` deliberately doesn't define one.
- Media is mounted read-**write** on purpose. The Movies, Shows, Adult and
  Collections libraries have `SaveLocalMetadata` enabled, so Jellyfin writes
  `.nfo` files and artwork next to the media. Mounting `:ro` would silently
  break metadata saving and subtitle downloads.
- Timezone is set with `TZ: Europe/London` rather than by bind-mounting
  `/etc/localtime`, which on macOS is a symlink into `/var/db/timezone` and
  dangles inside a Linux container.
- The container runs as root, the image default. Docker Desktop's virtiofs maps
  writes back to `tomwolfe:staff` on the host, so file ownership stays correct.
- Nothing here is exposed to the internet. Ports are published on all
  interfaces so anything on the LAN can reach it, but no router port forwarding
  is configured.
- If clients behave oddly with redirects or remote streaming, the usual Docker
  fix is *Dashboard → Networking → "Use request host for published server URI"*
  — the container's internal IP (172.x) is otherwise advertised in some
  responses.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
