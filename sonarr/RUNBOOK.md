# Sonarr runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap

At the desk. Do the Jellyfin 12.0 upgrade and its full scan **first**
(`jellyfin/README.md`, "Sequencing with the *arr rename").

1. Deploy: merge — the `sonarr compose` workflow converges it on the push.
   Docker creates `~/Docker/sonarr/config` on first start.
2. Create the login **through the API, not the UI.** Because the auth
   method is declared in `compose.yaml`, Sonarr skips its first-run
   "set up authentication" page and goes straight to a login form with
   no user behind it. The API key in `config.xml` is the way in — one
   authenticated PUT to the host config creates the user. On the mini:

   ```sh
   cd ~/Docker/sonarr/config
   key=$(sed -n 's/.*<ApiKey>\(.*\)<\/ApiKey>.*/\1/p' config.xml)
   curl -s -H "X-Api-Key: $key" http://localhost:8989/api/v3/config/host \
     | jq --arg u USERNAME --arg p PASSWORD \
          '.username=$u | .password=$p | .passwordConfirmation=$p' \
     | curl -s -o /dev/null -w '%{http_code}\n' -X PUT -d @- \
          -H "X-Api-Key: $key" -H 'Content-Type: application/json' \
          http://localhost:8989/api/v3/config/host/1
   ```

   `202` means the user exists; log in, then mirror the credentials to
   1P `sonarr-webui`. Verified against the pinned image.
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

## Upgrading

State lives outside the container, so an upgrade is a tag bump —
`sonarr.db` migrates forward on boot, and downgrading after a migration
means restoring a backup, not re-pulling the old tag. Sonarr's built-in
updater is inert in the linuxserver image (the container is the
release), which is the point: the version is the tag in `compose.yaml`.

```sh
cd sonarr/backup && dotnet run --project ../../build/src/Wolfe.Lab.Build -- backup && cd ../..   # snapshot first
# bump the tag (normal PR; the tick ships it)
```

Current stable tags: https://github.com/linuxserver/docker-sonarr/releases
(the `develop-…` ones are prereleases).

## Backup and restore

The nightly backup workflow snapshots `~/Docker/sonarr/config`
via the shared pipeline (`lab backup` from `sonarr/backup`, whose `ritten.json` declares what):
stops the container, `restic backup`, starts it. Excluded: `Backups/`
(Sonarr's own zips — restic is the backup), `MediaCover/` (artwork
TVDB re-serves), logs. Restore is the generic recipe in
`restic/README.md`; the thing the restore drill should assert exists is
`sonarr.db`.
