# Endpoints

Every service address in the lab. Hosts resolve via mDNS on the LAN;
`*.lab.twolfe.dev` names resolve via public DNS (wildcard record → the
mini, owned by `caddy/tofu`). Nothing is exposed to the internet.

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

## macmini.local

| Address | Service | Auth | Defined in |
| --- | --- | --- | --- |
| `:22` | SSH (macOS Remote Login) | 1Password SSH key via `~/.ssh/authorized_keys` | chezmoi (`private_dot_ssh`) |
| `:5900` | Screen Sharing | macOS account | System Settings |
| `:80` | Caddy — HTTP→HTTPS redirect | — | `caddy` |
| `:443` | Caddy — the front door (TLS, routes by hostname) | per-service (see rows below) | `caddy` |
| `:3000` | Forgejo — web UI + API | Forgejo account; API: 1P `forgejo-api-token` | `forgejo` |
| `:2222` | Forgejo SSH (git clone/push; container port 22) | SSH key registered in Forgejo profile | `forgejo` |
| `:3900` | Garage — S3 API | 1P `garage-tofu-state-key` (per-bucket keys) | `garage` |
| `:3903` | Garage — admin API | 1P `garage-s3-admin-token` | `garage` |
| `:8180` | Kestra — web UI + API | basic auth: 1P `kestra-admin`; webhook path key-authed | `kestra` |
| `:8096` | Jellyfin — web + clients | Jellyfin accounts | `jellyfin` |

Notes:

- Garage RPC (`:3901`) is internal to the container — deliberately not published.
- Jellyfin's discovery port (`7359/udp`) is published but non-functional through Docker Desktop's VM; 
  point clients at `jellyfin.lab.twolfe.dev` (or `macmini.local:8096`) manually.
- SSH sessions (port 22) don't get the login keychain or `path_helper` — Docker registry pulls and 
  other keychain-dependent operations only work in the GUI session (`:5900` or physically).

## Conventions

New service = new row, same commit — and a hostname row if it's proxied
(see caddy/README.md for the contract). If a port or a `caddy.caddyfile`
changes, this file changes with it.
