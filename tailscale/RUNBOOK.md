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
   chezmoi workflow (`00-install-packages.sh` on apply) can't provide it: once the Brewfile
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

## Removing a machine

The work MacBook, or any machine whose profile stops listing Tailscale.
Brew never uninstalls, so the Brewfile change only stops declaring it:

1. On the machine: `brew uninstall --cask tailscale-app`.
2. Admin console → Machines → remove it. The policy does not name
   individual workstations, so nothing in `tofu/` changes.
3. That machine can no longer reach `git.twolfe.dev` or
   `code.twolfe.dev` (both are tailnet addresses; the forgejo
   container's port 22 is not on the LAN). Point its Wolfe.Lab remote at
   the GitHub mirror, which Forgejo pushes to:
   `git remote set-url origin git@github.com:tom-wolfe/Wolfe.Lab.git`.

## The root

Once, in order. The seed step exists because a trust credential can only
name tags that already exist.

1. **Seed the tag.** Admin console → Access Controls: add
   `"tagOwners": {"tag:server": ["autogroup:admin"]}` to the policy and
   save. The root takes the whole file over at step 3; this line is only
   so step 2 can pick the tag.
2. **The credential.** Settings → Trust credentials → Generate OAuth
   client: scopes `policy_file`, `devices:core` and `dns`, all write, tag
   `tag:server`. Vault it as an API Credential item `tailscale-oauth`:
   `username` = client id, `credential` = client secret.
3. **Import the policy**, from a laptop, so the first plan is a diff
   against what the tailnet really has rather than an overwrite:

   ```sh
   cd tailscale/tofu
   run="op run --env-file ../../build/tofu-state.env --env-file secrets.env --"
   $run tofu init
   $run tofu import tailscale_acl.lab acl
   $run tofu plan
   ```

   Tripwire: the policy changes in place; **3 tags to add, 3 keys to
   change, 1 DNS preference to add**, nothing to destroy. Tailscale
   returns the console's policy with its own formatting, so the acl
   diff is the whole file — read it once.
4. **Merge.** `tailscale-tofu.yaml` applies. Or by hand: `tofu apply`
   from the same shell.
5. **Verify.** `tailscale status` from a laptop: the three servers show
   `tag:server` and their addresses are unchanged (`caddy/tofu` and
   `forgejo/tofu` carry them); `ssh macmini` over the tailnet still
   works. From the Pi, `curl -s http://macmini.tailf823b8.ts.net:8090/api/health`
   — the server↔server rule.

If step 4 refuses to tag a device (a 403 on the tags resource): a
credential scoped to a tag may only manage devices that already carry
it. Tag the three devices once in the console (Machines → … → Edit ACL
tags), re-run the plan (0 to add for tags), and the root holds them
from then on. The root is the source of truth either way; the console
is bootstrap.
