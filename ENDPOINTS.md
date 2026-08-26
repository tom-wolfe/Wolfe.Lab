# Endpoints

Every service address in the lab. All hosts resolve via mDNS on the LAN;
nothing is exposed to the internet.

## macmini.local

| Address | Service | Auth | Defined in |
| --- | --- | --- | --- |
| `:22` | SSH (macOS Remote Login) | 1Password SSH key via `~/.ssh/authorized_keys` | chezmoi (`private_dot_ssh`) |
| `:5900` | Screen Sharing | macOS account | System Settings |
| `:3000` | Forgejo — web UI + API | Forgejo account; API: 1P `Forgejo API Token` | `forgejo` |
| `:2222` | Forgejo SSH (git clone/push; container port 22) | SSH key registered in Forgejo profile | `forgejo` |
| `:3900` | Garage — S3 API | 1P `Garage tofu-state-key` (per-bucket keys) | `garage` |
| `:3903` | Garage — admin API | 1P `Garage S3 Admin Token` | `garage` |
| `:8180` | Kestra — web UI + API | basic auth: 1P `Kestra Admin`; webhook path key-authed | `kestra` |
| `:8096` | Jellyfin — web + clients | Jellyfin accounts | `jellyfin` |

Notes:

- Garage RPC (`:3901`) is internal to the container — deliberately not published.
- Jellyfin's discovery port (`7359/udp`) is published but non-functional through Docker Desktop's VM; 
  point clients at `macmini.local:8096` manually.
- SSH sessions (port 22) don't get the login keychain or `path_helper` — Docker registry pulls and 
  other keychain-dependent operations only work in the GUI session (`:5900` or physically).

## Conventions

New service = new row, same commit. If a port changes in a compose file, this table changes with it.
