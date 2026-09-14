# Tailscale

The lab's overlay network: a WireGuard mesh between the machines, run as
a host-level client on all three Macs. There is no compose stack and no
flow here — like chezmoi, this slice's artifact is host configuration
(one Brewfile line) plus the decisions recorded in this file.

## Why

Three things already written down wait on it:

- the neat public names (`jellyfin.twolfe.dev`) point at a Tailscale IP —
  the edge/DNS design in `caddy/README.md`;
- forgejo's portless clone URL waits on a dedicated IP with a free port
  22 (`forgejo/compose.yaml` records the decision);
- `100.64.0.0/10` isn't RFC1918, so the router's DNS-rebind filter has
  no objection to the `*.ts` names — they need no workaround at all.
  (The `*.lab` workaround in `caddy/README.md` stays: those names still
  resolve to RFC1918 space, by design — see below.)

## Decisions

- **The standalone app (`cask "tailscale-app"`), not the `tailscale`
  formula.** The formula is the open-source tailscaled: a root
  LaunchDaemon (`sudo brew services`), and macOS DNS integration —
  MagicDNS, the part this design leans on — is exactly what the app
  variants' Network Extension does properly. The mini's auto-login GUI
  session runs menu-bar apps happily (the same fact beszel's agent
  management banks on), so "headless" is no argument for the daemon.
- **All three machines, one Brewfile line**, in the all-machines section.
  The Mac Studio joins the same way when it lands; the Pi joins as a
  native Linux client.
- **MagicDNS on; no subnet router; no exit node.** Every machine that
  needs reaching runs the client, so advertising the LAN through the
  mini would only add an indirection whose failure looks like the
  network's. And the tailnet is *reachability*, not privacy — the one
  workload that wants a commercial VPN egress is the torrent client,
  handled inside the qbittorrent slice.
- **Tailscale's coordination server is an accepted cloud dependency.**
  Headscale is rejected for the bootstrap circularity: if the lab is
  down, you can't reach the lab to fix the thing you reach the lab
  through. (The data plane survives coordination outages; only logins
  and key operations stall.)
- **No tofu root — yet.** A first-party provider exists
  (`tailscale/tailscale`, OAuth-client auth) and could declare DNS
  preferences, the ACL and auth keys. Today that would codify two
  console toggles and a default allow-all ACL on a one-person tailnet.
  The moment the ACL carries real intent — node sharing (the
  file-sharing roadmap item), the Studio, anything multi-user — this
  slice grows a `tofu/` like the others.

## How the lab uses it

1. **Two wildcards, not a repoint.** Repointing `*.lab` at the Tailscale
   address would have cut off non-tailnet LAN devices (a TV Jellyfin app,
   guests), so the lab runs TWO wildcards: `*.lab` stays on the LAN address,
   `*.ts.twolfe.dev` points at 100.x. Every device has an option; one
   cert carries both SANs; every route snippet matches both names. The
   mechanics live in the caddy slice (`tofu/records.tf` records the
   decision). Consequence: the router's DHCP-DNS workaround STAYS —
   `*.lab` still resolves to RFC1918 — and both it and the dual
   wildcard retire together if local DNS on a Pi ever lands
   (ROADMAP.md).
2. **The laptops are enrolled in beszel** (beszel/README.md "The
   laptops"): agents dial the hub at the mini's
   MagicDNS name over the tailnet. Status alerts OFF for machines that
   are allowed to sleep.
3. **Per-service sidecar IPs.** forgejo is the first customer: a userspace `tailscale/tailscale` sidecar in the
   forgejo container's network namespace gives it its own tailnet seat
   with port 22 free, and clone URLs go portless at `git.twolfe.dev`
   (an A record in forgejo/tofu at the sidecar's address — the
   per-service neat-name pattern from the edge/DNS design). Design and
   bootstrap in forgejo/README.md "Tailnet identity"; further customers
   as they prove worth a dedicated address.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
