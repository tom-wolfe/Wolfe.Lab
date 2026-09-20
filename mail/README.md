# mail

Proton Bridge as a slice: an IMAP and SMTP door onto the Proton mailbox,
on the `lab` network, for the lab's own use. Nothing else in the lab
could read or send mail before this.

What it is for is the event scanner — detecting events in incoming mail
and putting them in Proton Calendar, which Proton itself only does for an
attached invitation. That is the watcher, and it is the next thing to
land here; this slice is the door it needs.

## Why Bridge can be a slice now

Proton offers SMTP submission only on business plans, so a personal plan
reaches its own mailbox through Bridge or not at all. Bridge was once
dismissed as "a logged-in session that has to live somewhere" — the same
shape as `op` on the mini, which went badly. In a container that
objection mostly dissolves: the credential lives in a volume rather than
a session, the login is a one-time `docker run` rather than a standing
terminal, and the thing that keeps it alive is `restart: unless-stopped`
like every other service.

## The image is community work, and the pin matters more than usual

Proton ships no container. The image most guides name,
`shenxn/protonmail-bridge`, stopped at Bridge 3.19.0 in April 2025 while
upstream is on 3.27.0 — and **Proton cuts off Bridge versions that fall
far enough behind**, so a stale image is not merely old, it eventually
stops being able to log in at all.

This slice pins the maintained fork instead. It tracks upstream closely,
and it disables the bridge's own auto-updater — which on arm64 downloaded
an amd64 build and crashed, so the mini is exactly the machine that bug
was about. Version management is the image tag, which is how the rest of
the lab pins anything.

Renovate (ROADMAP #5) is not running yet, so nothing watches this tag.
Until it does, the honest position is that this is an unwatched
dependency on a third party's spare time, and the failure mode is a login
that stops working rather than a container that stops running.

## What the volume holds, and why it is not backed up

A live Proton session for the primary mail account, in a GPG-protected
`pass` store. `KEYRING_PASSPHRASE` comes from the vault and encrypts the
key guarding it, so the volume is not readable on its own.

There is no `backup` section, deliberately. Backing it up would put a
live credential for the main mail account into restic and therefore into
B2, and the volume is entirely re-creatable: losing it costs one
interactive login, which `RUNBOOK.md` documents anyway. The cost is that
a restore of the mini is not complete until somebody logs in again.

## Reachability

No published ports and no route through the front door. Bridge binds
`127.0.0.1` inside the container and the image mirrors 143 and 25 onto
`0.0.0.0` with socat, so a container on the `lab` network reaches it at
`bridge:143` and `bridge:25` — and nothing outside that network reaches
it at all. The watcher will be the only client.

Nothing in Gatus watches this yet for the same reason: there is no port
to probe from the Pi. The watcher is what will carry the check, because
"can the lab still read its mail" is a question about the whole path
rather than about whether a container is running.

## Order of operations

The interactive login comes first and cannot be automated — it needs a
password, a second factor and a mailbox password. `RUNBOOK.md` has it.
Deploying before that login leaves a container that runs and serves
nothing.
