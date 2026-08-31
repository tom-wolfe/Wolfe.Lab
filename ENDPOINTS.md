# Endpoints

Every service address in the lab. Hosts resolve via mDNS on the LAN;
`*.lab.twolfe.dev` names resolve via public DNS (wildcard record → the
mini's LAN address, owned by `caddy/tofu`). Every hostname below also
has a `*.ts.twolfe.dev` twin resolving to the mini's Tailscale address —
same front door, for tailnet devices anywhere; prefer it off the LAN.
Nothing is exposed to the internet.

## Hostnames (via the Caddy front door)

TLS terminates at Caddy on `:443`; each name routes to the container named
in the owning slice's `caddy.caddyfile`. Prefer these in browsers; the
port addresses below remain the automation path and the fallback when the
front door is down.

| Hostname                         | Service                     | Upstream       |
| -------------------------------- | --------------------------- | -------------- |
| `https://forgejo.lab.twolfe.dev` | Forgejo                     | `forgejo:3000` |
| `https://kestra.lab.twolfe.dev`  | Kestra                      | `kestra:8080`  |
| `https://jellyfin.lab.twolfe.dev` | Jellyfin                     | `jellyfin:8096` |
| `https://s3.lab.twolfe.dev`      | Garage S3 (path-style only) | `garage:3900`  |
| `https://beszel.lab.twolfe.dev`  | Beszel monitoring hub       | `beszel:8090`  |
| `https://qbittorrent.lab.twolfe.dev` | qBittorrent web UI      | `gluetun:8080` |

## macmini.local

| Address | Service | Auth | Defined in |
| --- | --- | --- | --- |
| `:22` | SSH (macOS Remote Login) | 1Password SSH key via `~/.ssh/authorized_keys` | chezmoi (`private_dot_ssh`) |
| `:5900` | Screen Sharing | macOS account | System Settings |
| `:80` | Caddy — HTTP→HTTPS redirect | — | `caddy` |
| `:443` | Caddy — the front door (TLS, routes by hostname) | per-service (see rows below) | `caddy` |
| `:3000` | Forgejo — web UI + API | Forgejo account; API: 1P `forgejo-api-token` | `forgejo` |
| `:3900` | Garage — S3 API | 1P `garage-tofu-state-key` (per-bucket keys) | `garage` |
| `:3903` | Garage — admin API | 1P `garage-s3-admin-token` | `garage` |
| `:8090` | Beszel — hub UI + API | Beszel superuser account; `/api/health` is unauthenticated | `beszel` |
| `:8180` | Kestra — web UI + API | basic auth: 1P `kestra-admin`; webhook path key-authed | `kestra` |
| `:8096` | Jellyfin — web + clients | Jellyfin accounts | `jellyfin` |

## git.twolfe.dev (tailnet-routed)

Forgejo's own seat on the tailnet — a tailscale sidecar sharing the
container's network namespace, so git gets a portless clone URL. The
record (`forgejo/tofu`) points at the *sidecar's* Tailscale address, not
the mini's: resolution needs the internet like every name here, routing
needs the tailnet. `forgejo.tailf823b8.ts.net` is the same endpoint by
its DNS-independent MagicDNS name (forgejo/README.md "Tailnet identity").

| Address | Service | Auth | Defined in |
| --- | --- | --- | --- |
| `:22` | Forgejo SSH — `git@git.twolfe.dev:<user>/<repo>.git` | SSH key registered in Forgejo profile | `forgejo` |
| `:8080` | qBittorrent — web UI (via gluetun's namespace) | qBittorrent account (1P `qbittorrent-webui` is a copy) | `qbittorrent` |

Notes:

- Garage RPC (`:3901`) is internal to the container — deliberately not published.
- The Beszel agent is a HOST process, not a container. It binds
  `127.0.0.1:45876` only and nothing connects to it — it dials the hub
  outbound — so it is deliberately not a LAN endpoint.
- Jellyfin's discovery port (`7359/udp`) is published but non-functional through Docker Desktop's VM; 
  point clients at `jellyfin.lab.twolfe.dev` (or `macmini.local:8096`) manually.
- SSH sessions (port 22) don't get the login keychain or `path_helper` — Docker registry pulls and 
  other keychain-dependent operations only work in the GUI session (`:5900` or physically).

## Conventions

New service = new row, same commit — and a hostname row if it's proxied
(see caddy/README.md for the contract). If a port or a `caddy.caddyfile`
changes, this file changes with it.
