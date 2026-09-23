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
  22 (`forgejo/compose/compose.yaml` records the decision);
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
- **A tofu root (`tofu/`) carries the intent, and only the intent.** The
  policy file, one tag, the always-on nodes' tags and key expiry, and the
  MagicDNS toggle — the things the repo already leans on and that were
  console clicks until now. Not in it: auth keys (the sidecar's is minted
  by hand and spent once), global nameservers (the default), split DNS
  (arrives with local DNS on a Pi — ROADMAP.md). Applied like every root:
  `.forgejo/workflows/tailscale-tofu.yaml` on the push, a daily plan for
  drift.

## The policy

`tofu/policy.hujson` is the whole tailnet policy; the console shows it
verbatim, comments included. It says four things:

- **`tag:server`** on the always-on nodes (`tofu/variables.tf`
  `servers`: the mini, the Pi, the forgejo sidecar). A tagged device
  belongs to its tag rather than to a user, which is what a server is:
  nobody is logged in to vouch for it. Tagged devices don't expire their
  node key, and `tofu/devices.tf` declares that rather than relying on
  the default — a server whose key silently expires drops off the tailnet
  with nothing to notice, and `caddy/tofu` and `forgejo/tofu` carry
  tailnet addresses as record targets.
- **`tag:hybrid`** on the Studio (`hybrids`): a workstation that also
  serves while it is on (ROADMAP.md #7). Tagged for the same reasons as a
  server — it is a node, and its runner cannot re-auth interactively —
  but not `tag:server`, because servers reach each other on every port
  and the desk should be reachable only on the ports it serves. The cost
  of tagging it is its user identity: Taildrop to and from my own devices
  stops working there.
- **Workstations are not a tag.** Tagging a laptop would strip its user
  identity and the interactive re-auth that goes with it; "my devices" is
  `autogroup:member`, which is every user-owned device and no tagged one.
- **The rules:** my devices reach every node; servers reach each other;
  the Studio reaches every server, as my devices do. Servers reach the
  Studio only on the ports it serves, one rule per port, added with the
  thing that serves it. Nothing reaches a workstation, from anywhere. The
  server↔server rule is what the lab's own traffic rides — the Pi's
  Beszel agent to the hub, Gatus to the mini, caddy to the Pi, the Pi to
  the sidecar and to the mini's sftp — and the day one of those needs
  narrowing is the day the policy grows a line.

The tofu root's OAuth client carries `tag:server` only; `tag:hybrid`
lists `tag:server` among its owners, which is what lets that client
apply it without being re-minted.

A new server is one entry in `servers`, a new hybrid one in `hybrids`;
a new workstation is nothing.

**If a bad policy ever locks the lab out of itself,** the admin console is
the out-of-band fix — that is the coordination server being a cloud
dependency, working in the lab's favour. The apply itself can't be
stranded: the mini's runner reaches Forgejo over loopback, the state over
the LAN and the API over the internet, none of them through the tailnet.

## How the lab uses it

1. **One wildcard, at the Tailscale address.** `*.twolfe.dev` points at
   100.x and that is the only path in. The lab used to run two wildcards
   — a LAN one alongside it — because repointing everything at the
   tailnet would have cut off devices that cannot join it, a TV's
   Jellyfin app being the case that mattered. That premise no longer
   holds: the TV is deliberately dumb and offline, and Jellyfin is
   watched on an Apple TV that IS on the tailnet. Nothing left in the
   house needs the LAN path, so every service went down to one name.
   At home this costs nothing — Tailscale connects directly over the LAN
   rather than relaying. The mechanics live in the caddy slice
   (`tofu/records.tf` records the decision). Consequence: the router's
   DHCP-DNS workaround is probably retired with it, since the names now
   resolve to 100.x rather than RFC1918 — verify before removing it,
   because some routers rebind-protect the CGNAT range too
   (ROADMAP.md).
2. **The Pi is a native Linux client** — `tag:server`, like the mini —
   and its lab traffic rides the tailnet: the Beszel agent reaches the hub
   over it (beszel/README.md "The Pi"), Gatus probes the mini by its
   MagicDNS name (gatus/README.md "Placement"), and its restic snapshots
   go to the mini over SFTP (restic/README.md "From a Linux node").
3. **Per-service sidecar IPs.** forgejo is the first customer: a userspace `tailscale/tailscale` sidecar in the
   forgejo container's network namespace gives it its own tailnet seat
   with port 22 free, and clone URLs go portless at `git.twolfe.dev`
   (an A record in forgejo/tofu at the sidecar's address — the
   per-service neat-name pattern from the edge/DNS design). Design and
   bootstrap in forgejo/README.md "Tailnet identity"; further customers
   as they prove worth a dedicated address.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
