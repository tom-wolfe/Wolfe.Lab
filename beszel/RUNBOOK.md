# Beszel runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap (one-time, in order)

The ordering is forced: the hub must exist before the token does, and the
token must be in the vault before the agent can start. Same chicken-and-egg
as `garage/tofu` — run once, harvest the outputs, store them.

> **Create the `beszel-agent` item in 1Password before the agent's first
> deploy**, with placeholder values if you like — the real ones come from
> the hub in step 3. A deploy resolves both fields; without the item it
> fails, but only the beszel agent workflow fails: nothing else reads it.

1. **Bring the hub up.** `./setup.sh` from the repo root does it, or on the
   mini `docker compose --project-directory <this dir> up -d`. Caddy must
   already have been deployed once (it owns the `lab` network).
2. **Create the superuser.** First visit to http://macmini.local:8090
   prompts for it. Put it in 1Password as you would any login.
3. **Harvest the pair.** Settings → Tokens & Fingerprints. The universal
   token is **off by default** — enable it, and mark it **persistent**, or
   it expires after an hour and on every hub restart. Copy the token and
   the public key shown inline beside it into a `beszel-agent` API
   Credential item (`credential` and `username` respectively).

   Note what the token is: a *registration* token. Once an agent has
   enrolled it keeps working without it, so a stale vaulted token only
   bites when you enrol a new machine or wipe `~/.cache/beszel`. That's
   also why persistence matters — the laptops enrol months from now.
4. **Install the binary**: the chezmoi workflow does it from the Brewfile
   (the Macs) or the pinned download (the Pi); by hand, `brew bundle
   install --file ~/.Brewfile`.
5. **Start the agent**: run the **beszel agent** workflow, or on the node
   `cd beszel/agent && lab deploy --node <node>`. It resolves the token
   and key from the vault into the agent's unit and starts it.
6. **Confirm enrolment.** The mini should appear in the hub within a few
   seconds. If it doesn't, `tail ~/.cache/beszel/beszel-agent.log`.
7. **Configure notifications and thresholds** (see "Alerting"). Nothing
   alerts until you do — the hub ships no default thresholds.

## Upgrading

Two pins, and they should move together — the hub and agent speak a
versioned protocol and are only tested as a pair:

1. `image:` in `compose.yaml` (hub) — pinned, bumped by hand.
2. On the Macs the agent has **no version to pin**: the tap ships a single
   `beszel-agent.rb` regenerated on each release, so there is nothing
   versioned to name in the Brewfile. On Linux nodes it does: the release
   URL and checksum in `.chezmoiexternal.toml.tmpl`, bumped in the same
   commit as the hub image.

So the agent moves when you upgrade packages on the mini by hand — nothing
upgrades on a schedule there (`chezmoi/README.md`). Do it through bundle,
not plain `brew upgrade`, specifically for services like this one:

```sh
brew bundle install --file ~/.Brewfile --upgrade
```

Either way the old process keeps running the old binary until something
restarts it — a monitor silently running stale code is the exact failure
worth avoiding. So **run the beszel agent workflow afterwards**: the
binary's timestamp is part of each agent's unit, so an upgraded binary is
a changed unit and the deploy restarts it. Bump the hub's `image:` pin in
the same sitting, so the pair moves together.

If the agent does get ahead of the hub, a protocol mismatch is loud rather
than silent — the mini drops off the dashboard, and Gatus
and `~/.cache/beszel/beszel-agent.log` both say so. To hold it, `brew pin
beszel-agent` on the mini; upgrades skip pinned packages.

Bump the image via a normal PR; the push deploys it. Check the release notes first — the hub migrates its SQLite
schema forward on boot and downgrades are not supported, so going back means
restoring a backup, which is why the backup records its image tag.
