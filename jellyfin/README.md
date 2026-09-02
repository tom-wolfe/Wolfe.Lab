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

The `lab.jellyfin/deploy` flow in Kestra converges this stack after every green
`lab.chezmoi/update` tick — merge a compose change and it lands within one tick
(≤15 min). Manual converge: run the flow from the Kestra UI, or on the mini:

```sh
"$(chezmoi source-path)/../../jellyfin/flows/deploy/script.sh"
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
cd "$(chezmoi source-path)/../../jellyfin"

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

## After a reboot

Docker Desktop is set to start at login (Settings → General → "Start Docker
Desktop when you sign in"), and the mini auto-logs-in, so the stacks come back
on their own after a reboot. If Docker Desktop has quit for any other reason
(it did on 2026-09-02, without a reboot), bring it back by hand:

```sh
open -a Docker && "$(chezmoi source-path)/../../jellyfin/flows/deploy/script.sh"
```

Note this is tied to **signing in**, not to boot — a Mac mini sitting at the
login screen after a power cut won't run Jellyfin.

## Upgrading

State lives outside the container, so upgrades are a tag bump. **Back up first**
— an upgrade runs irreversible database migrations, and rolling back to an older
image afterwards will fail.

```sh
"$(chezmoi source-path)/../../scripts/backup.sh" jellyfin   # snapshot first
# bump the image tag in compose.yaml (normal PR; the tick ships it), then
# either let lab.jellyfin/deploy converge it or, by hand:
cd "$(chezmoi source-path)/../../jellyfin"
docker compose pull
docker compose up -d
docker compose logs -f           # watch migrations complete
```

Check what's current:

```sh
curl -s "https://api.github.com/repos/jellyfin/jellyfin/releases/latest" | grep '"tag_name"'
```

## Backup

Runs itself: the `lab.jellyfin/backup` flow fires nightly at 03:35
(`flows/backup/`). Manual snapshot — run the flow from the Kestra UI, or:

```sh
"$(chezmoi source-path)/../../scripts/backup.sh" jellyfin                  # config + database + library roots + plugins
"$(chezmoi source-path)/../../scripts/backup.sh" jellyfin --with-metadata  # the above plus ~670 MB of posters/fanart
```

Writes a snapshot into the restic repo on `/Volumes/Data2` (the image tag
rides on it as a snapshot tag; `lab.restic/offsite` ships it to B2 and
owns retention — see `restic/README.md`). It refuses to run if the drive
isn't mounted — an unmounted `/Volumes` path on macOS silently writes to
the internal disk. It stops the container first — SQLite copied mid-write
can be inconsistent — so expect ~30s of downtime; if a concurrent deploy
restarts the stack mid-snapshot, the snapshot is discarded and the run
fails loudly rather than keeping a suspect copy.

The default skips `metadata/` deliberately: it's ~670 MB of artwork that TMDB
will re-fetch, and the internal SSD only has ~46 GB free. What it does capture is
the part you can't get back: users, watch history, favourites, collections and
the library definitions.

### Restore

```sh
cd "$(chezmoi source-path)/../../jellyfin"
op run --env-file=../restic/restic.env -- restic snapshots --tag service:jellyfin
docker compose down
op run --env-file=../restic/restic.env -- restic restore <id> --target /tmp/restore
mv ~/Library/Application\ Support/jellyfin ~/Library/Application\ Support/jellyfin.bak
mv "/tmp/restore/Users/tomwolfe/Library/Application Support/jellyfin" \
   ~/Library/Application\ Support/
docker compose up -d
```

(restic reproduces the snapshot's full original path under `--target`,
hence the nested `mv`.) If you restore a metadata-less snapshot over a
wiped directory, artwork will be missing until the "Refresh Metadata"
task re-downloads it. Move the old directory aside rather than deleting
it, so you can copy `metadata/` back.

Make sure the image tag in `compose.yaml` matches the version the backup was
taken with (the `image:` tag on the snapshot records it).

## Rolling back to the native app

**This door is now closed.** On 2026-08-18 the container was upgraded to
10.11.11, which applied three irreversible schema migrations. `Jellyfin.app` is
still at 10.11.8 and will refuse to open the migrated database.

Rolling back now means restoring a pre-upgrade backup as well:

```sh
cd "$(chezmoi source-path)/../../jellyfin" && docker compose down
mv ~/Library/Application\ Support/jellyfin ~/Library/Application\ Support/jellyfin.bak
tar -xzf ~/Docker/jellyfin/backups/jellyfin-20260818-191721.tar.gz \
    -C ~/Library/Application\ Support/
cp -R ~/Library/Application\ Support/jellyfin.bak/metadata \
      ~/Library/Application\ Support/jellyfin/          # artwork isn't in the archive
open -a Jellyfin
```

Then re-add Jellyfin to *System Settings → General → Login Items* if you want it
starting itself again.

The equivalent applies to future upgrades: rolling back an image tag alone is
not enough once migrations have run, which is why `backup.sh` runs first.

## Pre-existing issues found during migration

Neither of these was caused by the move, and neither was changed:

- **4,178 items point at volumes that no longer exist** — 3,421 under
  `/Volumes/books` and 757 under `/Volumes/video`. The current drives mount as
  `/Volumes/Data1` and `/Volumes/Data2`, so these are stale rows from an earlier
  drive layout, most likely duplicates of content now under `Data1`. They show
  in totals (e.g. `Items/Counts` reports 365 movies where a user sees 135) but
  can't be played. Cleaning them up means removing the dead paths from the
  affected libraries and rescanning.
- **~180 MB of dead database files** in `data/`: `library.db.old`,
  `library.db-wal`, `library.db-shm`. Leftovers from the 10.10 → 10.11 migration
  in December 2025. Jellyfin 10.11 uses `jellyfin.db` and never reads these;
  they're excluded from backups and safe to delete.

Also worth noting: keep an eye on free space on `/Volumes/Data1` — it holds
the media libraries *and* every slice's nightly backups (10 archives each).
There is no automated check yet; that arrives with the monitoring slice
(see `ROADMAP.md`).

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
