# Radarr — the movie renamer

Radarr, pointed at the existing Movies library with **no indexers and
no download client** — Phase A of `ROADMAP.md` item 5, the movies half
of the rename that `sonarr/` does for TV. Everything about the shape
(host-path mounts, declared auth, no healthcheck, the backup pipeline)
is the same as `sonarr/README.md` and is not repeated here; this file
covers what is different about movies.

| | |
|---|---|
| Web UI | https://radarr.twolfe.dev (fallback http://macmini.local:7878) |
| Image | `linuxserver/radarr` pinned in `compose.yaml` |
| State | `~/Docker/radarr/config` — `radarr.db` (movies, root folders, naming), `config.xml` (API key, auth) |
| Root folders | `/Volumes/Data2/videos/movies`; `/Volumes/Data1/video/movies` is the Movies library's other root and is empty |
| Backups | `radarr-backup.yaml`, nightly at 02:45, into the restic repo (`restic/README.md`); the restore drilled straight after |

## What is different about movies

**Radarr requires a folder per movie**, full stop — the wiki FAQ answers
"can all my movie files be stored in one folder" with "No" and calls a
flat layout "highly unlikely" ever. Before the fold, the Data2 root held
312 entries of which **250 were loose files** in the root — the
`Alien.Covenant.2017.1080p.WEB-DL…[EtHD].mkv` kind, with Jellyfin's
`-poster.jpg`/`-backdrop.jpg` sidecars beside them. Library Import would
simply not see those.

So there is one bespoke step after all, done once at the desk: each
loose video becomes `<stem>/<stem>.<ext>`, sidecars with the same stem
ride along, and existing folders are left alone. Same-filesystem
renames — instant, no media bytes touched — and Radarr's importer then
reads the title and year out of the folder name exactly as it does for
the 62 movies that already had folders. It is a one-off; nothing in the
lab runs it again.

The other difference: Radarr renames **movie folders** the same way
Sonarr renames series folders — never automatically, only via
*Movies → Edit Movies → select → Edit → Root Folder set to the SAME root
→ "Yes, move the files"*. The default `Movie Folder Format` is
`{Movie Title} ({Release Year})`, which is what Jellyfin wants; that
step is what turns `10 Cloverfield Lane 2016 1080p HDRip x264 AAC-JYK/`
into `10 Cloverfield Lane (2016)/`.

## Secrets

**`radarr-webui`** (Login item), a copy not the origin, and the API key
in `config.xml` — identical standing to `sonarr/README.md`.

## Deliberately not configured

Indexers, download client, Connect → Jellyfin: Phase B, as for sonarr.
The `Adult` library (`/Volumes/Data2/videos/adult`) is a movies-type
library Radarr *could* manage as a third root; not added — that's a
separate call, not a default.

## Notes

- After Radarr renames a file, the `-poster.jpg` and `.nfo` sidecars
  that the fold carried into the folder no longer share its
  stem. Jellyfin re-saves artwork and NFO under the new name on the next
  scan (`SaveLocalMetadata` is on for Movies); the originals are
  orphans to sweep afterwards, not a loss.
- Data2 is APFS, so none of the exFAT caveats in `sonarr/README.md`
  apply to the movies root today. They would if Data1's root were used.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
