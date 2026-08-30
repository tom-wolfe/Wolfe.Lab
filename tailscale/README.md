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
- `100.64.0.0/10` isn't RFC1918, so the router's DNS-rebind filter has no
  objection to it and the DHCP-advertised-resolver workaround
  (`caddy/README.md`) can eventually be retired.

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
   not `via DERP`. A DERP path that persists means NAT traversal is
   still broken, which is what step 1 was for. The CLI lives at
   `/Applications/Tailscale.app/Contents/MacOS/Tailscale`.

## After it works — follow-ups, each its own change

1. **Repoint the wildcard**: `lab_ipv4` in `caddy/tofu` → the mini's
   100.x address. That is the remote-access migration: `*.lab` names
   then answer wherever the tailnet reaches — and stop answering for
   LAN devices that are NOT on it (the TV's Jellyfin app, guests).
   Decide that trade deliberately, and apply it at the desk: if the
   tunnel is flaky, name resolution for the whole lab goes with it.
2. **Retire the router workaround**: with `*.lab` resolving into
   100.64/10 the rebind filter has nothing to object to, so the
   DHCP-advertised 1.1.1.1/8.8.8.8 can revert to default. Verify at the
   desk before touching it.
3. **Enroll the laptops in beszel** — WebSocket mode was chosen exactly
   so agents could dial the hub across the tailnet
   (`chezmoi/home/dot_config/beszel/`). Status alerts OFF for machines
   that are allowed to sleep.
4. **Later era**: per-service sidecar IPs — forgejo's port-22 clone URL
   is the first customer.
