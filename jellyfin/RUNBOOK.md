# Jellyfin runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## After a reboot

Docker Desktop is set to start at login (Settings → General → "Start Docker
Desktop when you sign in"), and the mini auto-logs-in, so the stacks come back
on their own after a reboot. If Docker Desktop has quit for any other reason
(it has, without a reboot), bring it back by hand:

```sh
open -a Docker && lab deploy
```

Note this is tied to **signing in**, not to boot — a Mac mini sitting at the
login screen after a power cut won't run Jellyfin.

## Upgrading

State lives outside the container, so upgrades are a tag bump. **Back up first**
— an upgrade runs irreversible database migrations, and rolling back to an older
image afterwards will fail.

```sh
cd jellyfin/backup && lab backup && cd ../..   # snapshot first
# bump the image tag in compose.yaml (normal PR; the tick ships it), then
# either let the jellyfin compose workflow converge it or, by hand:
cd ~/.local/share/Wolfe.Lab/jellyfin
docker compose pull
docker compose up -d
docker compose logs -f           # watch migrations complete
```

Check what's current:

```sh
curl -s "https://api.github.com/repos/jellyfin/jellyfin/releases/latest" | grep '"tag_name"'
```

## Backup

Runs itself: `.forgejo/workflows/jellyfin-backup.yaml` snapshots this
slice nightly at 02:35 and drills the restore straight after (the `backup` section of
`ritten.json` declares what: config, database, library roots and
plugins; `metadata/` is ~670 MB of artwork a "Refresh Metadata"
re-downloads, so it is excluded). Manual snapshot — run the backup
workflow from the Actions tab, or:

```sh
cd jellyfin/backup
lab backup
```

Writes a snapshot into the restic repo on `/Volumes/Data2` (the image tag
rides on it as a snapshot tag; `restic-offsite.yaml` ships it to B2 and
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

## Restore

```sh
cd jellyfin/backup
lab restore
```

`restic/RUNBOOK.md` "Restore" for what it does and its options. The
snapshot carries no `metadata/`, so artwork is missing until the
"Refresh Metadata" task re-downloads it — or copy `metadata/` back from
the `.bak` directory the job set aside.

The job refuses to restore onto an image other than the one the
snapshot was taken under; pin `compose.yaml` first.
