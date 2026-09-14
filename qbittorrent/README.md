# qBittorrent

The torrent client, containerized behind a NordVPN tunnel — and the reason
the mini itself is no longer on NordVPN.

## Why this slice exists

This is the Tailscale precondition from `ROADMAP.md`, built as its own
slice. The native Transmission.app relied on the host NordVPN app, and a
host VPN on macOS is all-or-nothing: Nord offers no app-level split
tunnelling there (Apple's Big Sur networking changes; the browser
extension is their only option), so *every* outbound connection the lab
made — git pulls, brew, lego's ACME calls, Pushover, the healthchecks.io
ping — traversed Nord. And behind Nord's shared NAT, Tailscale peers
would rarely hole-punch and would fall back to DERP relays.

So the dependency is inverted: the one app that needs a VPN moved into a
container that cannot reach the network *except* through the tunnel, and
the host got its real interface back. See `tailscale/README.md` for what
that unblocks.

## Why qBittorrent, not Transmission

Transmission was the incumbent, and containerizing it unchanged was the
smaller step. The swap was made deliberately, at the one moment it was
free (nothing deployed, no state worth keeping):

- **The *arr stack programs against the client's API** (`ROADMAP.md`,
  the media item's Phase B), and qBittorrent is the first-class client
  there: its Web API has native categories with per-category save paths
  and tags, which is exactly what Sonarr/Radarr build their
  completed-download handling on. Transmission is supported but its
  "categories" are faked as subdirectories, because the RPC offers
  nothing better.
- Richer seeding and ratio controls, and a web UI that is an actual
  management surface rather than a remote control.
- The alternatives don't beat it: Deluge (supported by *arr, but a
  clunkier daemon/UI split and plugin-based labels), rTorrent/ruTorrent
  (archaic to configure, overkill). A Usenet client (SABnzbd) is a
  different protocol and would land *alongside*, not instead.

One honest cost, see Secrets below: unlike the linuxserver Transmission
image, qBittorrent's web UI auth cannot come from env — the 1Password
pattern degrades from origin to copy for that one credential.

## The shape

Two containers, one network namespace:

- **gluetun** owns the namespace and the WireGuard (NordLynx) tunnel, and
  firewalls away everything that isn't the tunnel, the Docker network, or
  the web UI port.
- **qbittorrent** joins it with `network_mode: service:gluetun`. It has
  no network identity of its own: every packet goes through gluetun or
  nowhere. Tunnel down means qbittorrent is *offline*, not exposed — a
  kill switch by construction rather than by app feature.

Consequences worth knowing:

- The caddy route's upstream is `gluetun:8080`, not `qbittorrent:8080` —
  on the lab network the web UI answers at gluetun's address, because
  that is the namespace it lives in.
- `docker ps` showing gluetun unhealthy IS the tunnel being down (the
  image ships a connectivity probe). Nothing alerts on it — the lab rule
  about Docker healthchecks. Gatus checks the web UI answers
  (`gatus/config/lab.yaml`), which is not the same thing as the tunnel
  being up.
- **No inbound peers.** NordVPN offers no port forwarding, so nothing can
  dial in; qbittorrent only uploads on connections it opened itself.
  That is parity with the old host setup, not a regression.

## Secrets

- `gluetun.env` ← **`nordvpn-wireguard`** (Password item, `credential`
  field): the usual `create_` template
  (`chezmoi/home/Docker/qbittorrent/`), 1Password as the origin, written
  only while missing, rotation = delete + `chezmoi apply`. The key is
  the WireGuard private key from the Nord dashboard →
  *Manual configuration*. It is NOT the account password and NOT the
  OpenVPN service credential. If the dashboard only offers an access
  token, the key falls out of
  `curl -s -u token:<TOKEN> https://api.nordvpn.com/v1/users/services/credentials | jq -r .nordlynx_private_key`.
  Shape check before saving — this has bitten once: the KEY
  is 44 base64 chars ending `=`; the dashboard's access TOKEN is 64 hex
  chars, and gluetun rejects it as `wgtypes: incorrect key size: 48`.
  The item keeps that token in a separate `token` field — it is what
  mints a fresh key at rotation time.
- **`qbittorrent-webui`** (Login item) — a **copy, not the origin**.
  qBittorrent's web UI password is set in the UI and stored as a hash in
  its own qBittorrent.conf; there is no env or file to materialize it
  from. Same standing as the beszel-agent token: the vault mirrors state
  the app owns, and the nightly backup — not the vault — is what
  restores it. Rotation runs the other way: change it in the UI, update
  the item.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
