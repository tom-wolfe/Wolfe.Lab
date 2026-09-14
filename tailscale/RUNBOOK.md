# Tailscale runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap

In order. Step 1 is the precondition and happens at the desk.

1. **Get the mini off NordVPN** — the qbittorrent slice's bootstrap
   (`qbittorrent/README.md`). Behind Nord's shared NAT, peers rarely
   hole-punch and fall back to DERP relays: tolerable for SSH, poor for
   streaming, and streaming is one of the reasons this exists.
2. Install — **the mini FIRST, and before this branch merges.** The cask
   is a `.pkg`, which needs sudo, and the headless, sudo-less
   chezmoi workflow (`install-packages.sh` on apply) can't provide it: once the Brewfile
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
   upgrade can take longer (measured on Three — DERP for the
   whole ping window, direct under sustained traffic). Judge with
   `ping -c 30 <100.x address>` then `tailscale status | grep macmini`.
   DERP that survives THAT means traversal is broken — step 1
   territory, or a network that eats UDP (`tailscale netcheck` showing
   `UDP: false`). Escape hatch if some network ever needs it: the mini
   listens on UDP 41641, so a static forward on the router makes the
   home side unconditionally reachable. The CLI lives at
   `/Applications/Tailscale.app/Contents/MacOS/Tailscale`.
