# Radarr — the movie renamer

Radarr, pointed at the existing Movies library with **no indexers and
no download client** — Phase A of `ROADMAP.md` item 5, the movies half
of the rename that `sonarr/` does for TV. Everything about the shape
(host-path mounts, declared auth, no healthcheck, the backup pipeline)
is the same as `sonarr/README.md` and is not repeated here; this file
covers what is different about movies.

| | |
|---|---|
| Web UI | https://radarr.lab.twolfe.dev (fallback http://macmini.local:7878) |
| Image | `linuxserver/radarr` pinned in `compose.yaml` |
| State | `~/Docker/radarr/config` — `radarr.db` (movies, root folders, naming), `config.xml` (API key, auth) |
| Root folders | `/Volumes/Data2/videos/movies`; `/Volumes/Data1/video/movies` is the Movies library's other root and is empty |
| Backups | `lab.radarr/backup`, nightly 04:20, into the restic repo (`restic/README.md`) |

## What is different about movies

**Radarr requires a folder per movie**, full stop — the wiki FAQ answers
"can all my movie files be stored in one folder" with "No" and calls a
flat layout "highly unlikely" ever. On 2026-09-11 the Data2 root held
312 entries of which **250 were loose files** in the root — the
`Alien.Covenant.2017.1080p.WEB-DL…[EtHD].mkv` kind, with Jellyfin's
`-poster.jpg`/`-backdrop.jpg` sidecars beside them. Library Import would
simply not see those.

So there is one bespoke step after all, and it is `scripts/fold-movies.sh`:
each loose video becomes `<stem>/<stem>.<ext>`, sidecars with the same
stem ride along, and existing folders are left alone. Same-filesystem
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

## Bootstrap

At the desk, after the Jellyfin 12.0 upgrade and its full scan.

1. Fold the loose files, dry run first, then for real:

   ```sh
   cd "$(chezmoi source-path)/../.."
   DRY=1 radarr/scripts/fold-movies.sh /Volumes/Data2/videos/movies   # prints the plan
   radarr/scripts/fold-movies.sh /Volumes/Data2/videos/movies
   ```

   Anything reported as `skip:` (a loose file whose stem already exists
   as a folder) gets folded by hand.
2. Deploy: merge, wait for the tick (or trigger `lab.radarr/deploy`).
3. First visit: set the username and password, mirror to 1P `radarr-webui`.
4. *Settings → Media Management*: turn **Rename Movies** on (off by
   default); keep the formats. Add the Data2 root folder (and the Data1
   one if it ever gets movies).
5. *Movies → Library Import*: review every match — TMDb guesses from the
   folder name, and the `www.UIndex.org    -    Title 2025 …` folders are
   the ones to check — **Monitor: None**, any profile, import.
6. *Movies → Edit Movies → select all → Rename Files* (preview first),
   then the folder-rename recipe above for the folders that need it.
7. Then the single Jellyfin full scan that closes the pass
   (`sonarr/README.md`, bootstrap step 7).

## Deliberately not configured

Indexers, download client, Connect → Jellyfin: Phase B, as for sonarr.
The `Adult` library (`/Volumes/Data2/videos/adult`) is a movies-type
library Radarr *could* manage as a third root; not added — that's a
separate call, not a default.

## Upgrading, backup, restore

As `sonarr/README.md`, substituting `radarr`: tag bump after a snapshot
(`scripts/backup.sh radarr`), current stable tags at
https://github.com/linuxserver/docker-radarr/releases (`nightly-…` and
`develop-…` are prereleases), nightly backup at 04:20 excluding
`Backups/`, `MediaCover/` and logs. The file a restore drill should
assert is `radarr.db`.

## Notes

- After Radarr renames a file, the `-poster.jpg` and `.nfo` sidecars
  that `fold-movies.sh` carried into the folder no longer share its
  stem. Jellyfin re-saves artwork and NFO under the new name on the next
  scan (`SaveLocalMetadata` is on for Movies); the originals are
  orphans to sweep afterwards, not a loss.
- Data2 is APFS, so none of the exFAT caveats in `sonarr/README.md`
  apply to the movies root today. They would if Data1's root were used.
