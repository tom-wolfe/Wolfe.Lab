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
| Web UI | https://sonarr.twolfe.dev (fallback http://macmini.local:8989) |
| Image | `linuxserver/sonarr` pinned in `compose.yaml` |
| State | `~/Docker/sonarr/config` — `sonarr.db` (series, root folders, naming), `config.xml` (API key, auth) |
| Root folders | `/Volumes/Data1/video/shows`, `/Volumes/Data2/videos/shows` — the Shows library's two roots in `jellyfin/` |
| Backups | `sonarr-backup.yaml`, nightly at 02:50, into the restic repo (`restic/README.md`); the restore drilled straight after |

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

## Deliberately not configured

- **Indexers, download client** — Phase B. Sonarr shows a health
  warning about both; that warning is the design.
- **Connect → Jellyfin** ("update library on rename") — would fire a
  partial Jellyfin scan per rename during a pass meant to end in one
  scan. Worth adding in Phase B, when renames become one-at-a-time
  imports.
- **Trusted Networks / X-Forwarded-For** — only matter with the local-
  address auth bypass, which is off.

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

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
