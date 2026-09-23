# Radarr runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap

At the desk, after the Jellyfin 12.0 upgrade and its full scan.

1. Fold the loose files: for every video sitting loose in
   `/Volumes/Data2/videos/movies`, make a folder named for its stem and
   move it in with every same-stem sidecar (`<stem>.en.srt`,
   `<stem>-poster.jpg`, `<stem>-backdrop.jpg`). Same-filesystem renames,
   so it is instant and touches no media bytes. Skip the `._*` AppleDouble
   litter, and a loose file whose stem already exists as a folder gets
   folded by hand.
2. Deploy: merge — the `radarr compose` workflow converges it on the push.
3. Create the login through the API — the same recipe as
   `sonarr/README.md` bootstrap step 2 with `radarr` for `sonarr` and
   port `7878`. Then mirror to 1P `radarr-webui`.
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

## Upgrading, backup, restore

As `sonarr/README.md`, substituting `radarr`: tag bump after a snapshot
(`lab backup` from `radarr/`), current stable tags at
https://github.com/linuxserver/docker-radarr/releases (`nightly-…` and
`develop-…` are prereleases), nightly backup at 04:20 excluding
`Backups/`, `MediaCover/` and logs. The file a restore drill should
assert is `radarr.db`.
