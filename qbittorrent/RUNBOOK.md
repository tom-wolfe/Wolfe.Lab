# qBittorrent runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap

At the desk, not remotely — the last steps change the mini's default
route.

1. Create `nordvpn-wireguard` in the Wolfe.Lab vault (see Secrets).
2. Deploy: merge — the qbittorrent workflow converges it on the push.
3. Verify the tunnel from inside the namespace (the LSIO image has curl;
   gluetun's own image is shell-less):
   `docker exec qbittorrent curl -s https://ipinfo.io/ip` — expect a
   Nord egress address, not the house's WAN IP. Then prove the kill
   switch: `docker stop gluetun`, same curl times out, `docker start
   gluetun` (and restart qbittorrent if the UI stays unreachable — see
   the namespace note in compose.yaml).
4. First login and UI-held settings. The image prints a temporary admin
   password to `docker logs qbittorrent` on each start until a permanent
   one is set. Log in at `macmini.local:8080` and set, in
   Settings → Web UI:
   - a permanent password → mirror it to 1P `qbittorrent-webui`;
   - `qbittorrent.lab.twolfe.dev,qbittorrent.ts.twolfe.dev` in
     **Server domains** — qBittorrent validates the Host header, so the
     caddy routes 401 until this is set (the `macmini.local:8080`
     fallback keeps working regardless);
   and in Settings → Downloads, the default save path — a
   `/Volumes/Data1/...` path; the drives are mounted at their host
   paths, so what qbittorrent writes is what jellyfin sees.
6. Take the host off Nord — the point of the whole exercise. Quit the
   native Transmission.app (nothing worth keeping). In the NordVPN app: disable the kill switch,
   disconnect, log out. Then
   `brew uninstall --cask transmission nordvpn`. The Brewfile already
   stopped declaring both on the server, but `brew bundle` never
   uninstalls anything — this step is manual by design.
7. Verify the route is real again: `route -n get default` should show
   the LAN gateway on a real interface, not a `utun` via `10.5.0.2`.
   Then run the chezmoi and heartbeat workflows from the Actions tab and
   watch them stay green — that exercises git, 1Password and the
   heartbeat over the restored interface.

## Backup

The nightly backup workflow, from 02:20: cold restic snapshot of
`config/` — qBittorrent.conf (including the web UI password hash),
categories, and BT_backup/ (.torrent files + fastresume); the parts
configured in the UI rather than declared here. Mount-guarded,
integrity-guarded — the shared `scripts/backup.sh` pipeline, with this
slice's paths declared in `flows/backup/backup.conf`; retention and the
offsite copy belong to `restic-offsite.yaml` (`restic/README.md`).
`gluetun/` is deliberately excluded: a disposable server-list cache.

## Upgrading

Bump the pins in `compose.yaml` via a PR like everywhere else. gluetun in
particular: read its release notes — env var names and defaults genuinely
change between minor versions, and this container is the slice's security
boundary. The qbittorrent tag encodes both the app and libtorrent
versions (`5.2.3_v2.0.14`); stay on the libtorrent-2.x line unless
seeding behaviour gives a concrete reason for the `libtorrentv1`
variant.
