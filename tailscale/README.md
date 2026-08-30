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

## Bootstrap

In order. Step 1 is the precondition and happens at the desk.

1. **Get the mini off NordVPN** — the qbittorrent slice's bootstrap
   (`qbittorrent/README.md`). Behind Nord's shared NAT, peers rarely
   hole-punch and fall back to DERP relays: tolerable for SSH, poor for
   streaming, and streaming is one of the reasons this exists.
2. Install — **the mini FIRST, and before this branch merges.** The cask
   is a `.pkg`, which needs sudo, and the headless, sudo-less
   `lab.chezmoi/packages` flow can't provide it: once the Brewfile
   change reaches the mini, that flow fails on every tick (an instant
   sudo error, alerting each time) until the cask exists. Pre-empt it:
   `brew install --cask tailscale-app` once in an SSH or Screen Sharing
   session, after which the flow sees it installed and is a no-op.
   Upgrades are the app's own job — the cask is marked `auto_updates`,
   so brew's nightly upgrade deliberately skips it. Laptops: `chezmoi
   apply` runs the Brewfile as usual.
3. Sign in on each machine (Tailscale.app → browser SSO; the mini over
   Screen Sharing) and approve the VPN configuration prompt.
4. Admin console (login.tailscale.com): confirm MagicDNS is on (the
   default for new tailnets), and **disable key expiry on the mini** — a
   server whose node key silently expires drops off the tailnet with
   nothing to notice. The laptops re-auth interactively; they can keep
   expiry.
5. Verify from a laptop, ideally off the LAN (phone hotspot):
   `tailscale status`, then `tailscale ping macmini` — expect `direct`,
   not `via DERP`. But don't trust a DERP verdict from `tailscale ping`
   alone: it gives up after ~10 seconds, and through carrier CGNAT the
   upgrade can take longer (measured 2026-08-30 on Three — DERP for the
   whole ping window, direct under sustained traffic). Judge with
   `ping -c 30 <100.x address>` then `tailscale status | grep macmini`.
   DERP that survives THAT means traversal is broken — step 1
   territory, or a network that eats UDP (`tailscale netcheck` showing
   `UDP: false`). Escape hatch if some network ever needs it: the mini
   listens on UDP 41641, so a static forward on the router makes the
   home side unconditionally reachable. The CLI lives at
   `/Applications/Tailscale.app/Contents/MacOS/Tailscale`.

## After it works — follow-ups, each its own change

1. **Repoint the wildcard — SUPERSEDED (Tom's call, 2026-08-31).**
   Instead of repointing `*.lab` at the Tailscale address — which would
   have cut off non-tailnet LAN devices (a TV Jellyfin app, guests) —
   the lab runs TWO wildcards: `*.lab` stays on the LAN address,
   `*.ts.twolfe.dev` points at 100.x. Every device has an option; one
   cert carries both SANs; every route snippet matches both names. The
   mechanics live in the caddy slice (`tofu/records.tf` records the
   decision). Consequence: the router's DHCP-DNS workaround STAYS —
   `*.lab` still resolves to RFC1918 — and both it and the dual
   wildcard retire together if local DNS on a Pi ever lands
   (ROADMAP.md).
2. **Enroll the laptops in beszel — built 2026-08-31**, see
   beszel/README.md "The laptops": agents dial the hub at the mini's
   MagicDNS name over the tailnet. Status alerts OFF for machines that
   are allowed to sleep.
3. **Later era**: per-service sidecar IPs — forgejo's port-22 clone URL
   is the first customer.
