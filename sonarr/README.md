# Sonarr — the TV renamer

Sonarr, pointed at the existing Shows library with **no indexers and no
download client**. In that configuration it does exactly one job: parse
what is on the drives, match it against TVDB, and rename and re-file it
into one consistent layout. That is Phase A of `ROADMAP.md` item 5 — the
bespoke rename script the lab was otherwise going to write, as two
containers instead (this one and `radarr/`). Acquisition — Prowlarr, the
client hook-up, Jellyseerr — is Phase B and is deliberately not here.

| | |
|---|---|
| Web UI | https://sonarr.lab.twolfe.dev (fallback http://macmini.local:8989) |
| Image | `linuxserver/sonarr` pinned in `compose.yaml` |
| State | `~/Docker/sonarr/config` — `sonarr.db` (series, root folders, naming), `config.xml` (API key, auth) |
| Root folders | `/Volumes/Data1/video/shows`, `/Volumes/Data2/videos/shows` — the Shows library's two roots in `jellyfin/` |
| Backups | `lab.sonarr/backup`, nightly 04:05, into the restic repo (`restic/README.md`) |

## Why the paths look like jellyfin's

Same rule, same reason. Sonarr stores absolute paths — the root folders
and every episode file — in `sonarr.db`, and the rename it exists to do
is only meaningful if Sonarr and Jellyfin agree on what a path *is*. So
the media drives are bind-mounted at their host paths (`/Volumes/Data1`
→ `/Volumes/Data1`), not at the `/tv` the linuxserver docs suggest. The
config directory follows the ordinary `~/Docker/<slice>` convention.

## The renaming, honestly scoped

What Sonarr does well: rename **episode files** to
`Series Title - S01E01 - Episode Title Quality.mkv` inside
`Season 01/` folders. Its defaults are the layout Jellyfin documents, so
the naming settings are left at their defaults.

What it does on request only: rename the **series folder** itself.
Sonarr never renames an existing series folder automatically; the
wiki's recipe is *Series → Select Series → Edit → Root Folder set to
the SAME root → "Yes, move the files"*, which re-files the folder to the
`Series Folder Format`. That turns `Archer.2009.S01-S14.1080p.WEB.DD.AV1-DBMS`
into `Archer (2009)`.

What it does not do: consolidate the two roots into one (the drives are
different sizes and the split is a storage decision, not a naming one),
touch the `Adult`, `Courses` or `Home Videos` libraries (not TV), or
clean up the sidecars Jellyfin wrote next to the old filenames — see
"Notes".

## Secrets

- **`sonarr-webui`** (Login item) — a **copy, not the origin**, same
  standing as `qbittorrent-webui`: the password is set in the UI on
  first visit and hashed into `config.xml`; the nightly backup, not the
  vault, is what restores it. Rotation runs UI → vault.
- The **API key** lives in `config.xml`, minted by Sonarr on first boot.
  Nothing needs it until Phase B (Prowlarr, Jellyseerr); it rides in the
  backup and stays out of the vault until something has to consume it.

Authentication *mode* is not a secret and not a UI setting here — it is
declared in `compose.yaml` (`SONARR__AUTH__REQUIRED: Enabled`), because
the "disabled for local addresses" option would treat every request via
caddy as local.

## Bootstrap

At the desk. Do the Jellyfin 12.0 upgrade and its full scan **first**
(`jellyfin/README.md`, "Sequencing with the *arr rename").

1. Deploy: merge, wait for the tick (or trigger `lab.sonarr/deploy`).
   Docker creates `~/Docker/sonarr/config` on first start.
2. First visit shows the authentication setup page: choose a username
   and password, mirror them to 1P `sonarr-webui`.
3. *Settings → Media Management*: turn **Rename Episodes** on (it is off
   by default — without it nothing below renames anything). Leave the
   formats at their defaults. Add both root folders.
4. *Series → Library Import*: pick a root, review every match — TVDB
   guesses from folder names, and the scene-named ones
   (`[Coalgirls]_Elfen_Lied_…`, `House MD Season 1, 2, 3 … + Extras`)
   deserve a look — set **Monitor: None** (nothing to search for
   without indexers) and any quality profile, then import. Repeat for
   the other root.
5. Rename files, in one pass: *Series → Select Series → all → Rename
   Files*. The preview lists every change before it applies; read it.
6. Rename folders where the folder name is the mess: the Edit → same
   Root Folder → "Yes, move the files" recipe above, for the selected
   series.
7. Run `radarr/`'s bootstrap too, then **one** full library scan in
   Jellyfin — every renamed path is a new item ID, so the Shows and
   Movies libraries rebuild. Watch history is explicitly not wanted
   (ROADMAP item 5), so that is the whole cost.

## Deliberately not configured

- **Indexers, download client** — Phase B. Sonarr shows a health
  warning about both; that warning is the design.
- **Connect → Jellyfin** ("update library on rename") — would fire a
  partial Jellyfin scan per rename during a pass meant to end in one
  scan. Worth adding in Phase B, when renames become one-at-a-time
  imports.
- **Trusted Networks / X-Forwarded-For** — only matter with the local-
  address auth bypass, which is off.

## Upgrading

State lives outside the container, so an upgrade is a tag bump —
`sonarr.db` migrates forward on boot, and downgrading after a migration
means restoring a backup, not re-pulling the old tag. Sonarr's built-in
updater is inert in the linuxserver image (the container is the
release), which is the point: the version is the tag in `compose.yaml`.

```sh
"$(chezmoi source-path)/../../scripts/backup.sh" sonarr   # snapshot first
# bump the tag (normal PR; the tick ships it)
```

Current stable tags: https://github.com/linuxserver/docker-sonarr/releases
(the `develop-…` ones are prereleases).

## Backup and restore

`lab.sonarr/backup` snapshots `~/Docker/sonarr/config` nightly at 04:05
via the shared pipeline (`scripts/backup.sh`, `flows/backup/backup.conf`):
stops the container, `restic backup`, starts it. Excluded: `Backups/`
(Sonarr's own zips — restic is the backup), `MediaCover/` (artwork
TVDB re-serves), logs. Restore is the generic recipe in
`restic/README.md`; the thing the restore drill should assert exists is
`sonarr.db`.

## Notes

- **`/Volumes/Data1` is exFAT.** Three consequences. Colons and other
  characters exFAT forbids are replaced by Sonarr's "Colon Replacement"
  setting (default: delete) — fine, just not configurable per drive.
  Case-only renames (`the boys` → `The Boys`) go through VirtioFS to a
  case-insensitive volume; macOS handles those, but if one ever fails
  with "already exists", rename via a temporary name. And macOS writes
  `._name` AppleDouble files beside everything it touches on exFAT —
  Sonarr ignores them, Jellyfin ignores them, they are litter.
- **Sidecars Jellyfin wrote next to the old names** (`SaveLocalMetadata`
  is on for Shows: `.nfo` files and `-thumb.jpg` artwork) are not
  renamed with the video. Jellyfin re-identifies the renamed file and
  re-saves them under the new name; the old ones are orphans. Harmless,
  and a `find -name '*.nfo'` sweep once the scan is done clears them.
- The image runs as `PUID/PGID` 501:20 — the same VirtioFS ownership
  story as qbittorrent.
- Nothing here is reachable from the internet; the `.ts` twin is the
  off-LAN path.
