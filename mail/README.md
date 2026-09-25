# mail

Proton Bridge as a slice: an IMAP and SMTP door onto the Proton mailbox,
on the `lab` network, for the lab's own use. Nothing else in the lab
could read or send mail before this.

What it is for is the event scanner — detecting events in incoming mail
and putting them in Proton Calendar, which Proton itself only does for an
attached invitation. That is `watcher/`, and it is the other half of this
slice.

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

Renovate (ROADMAP #3) is not running yet, so nothing watches this tag.
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

## The watcher

Its own solution under `watcher/`, with its own dependencies and its own
tests, built into `lab/mail-watcher` by the compose component's deploy, which
declares it, and run beside bridge. It is the mail slice's artefact rather than part of the lab's
CLI: the CLI runs jobs and exits, and this is a process that stays up.

**It holds the connection open rather than polling.** IMAP IDLE means an
invitation is on its way back seconds after the booking arrives, instead
of at the top of the next hour — which is the difference between one
buzz and two. The cost is a connection to nurse: IDLE is capped at about
half an hour by the protocol and servers hang up when they like, so it is
a reconnect loop with backoff.

**A UID watermark says where to resume**, and it is what makes
reconnecting cheap: nothing that arrived during a disconnection is
missed, and nothing already handled is handled twice. A UID rather than a
date because IMAP's date search has *day* granularity — a date watermark
would re-read the whole of today, all day. `UIDVALIDITY` is stored beside
it, because a renumbered mailbox makes every stored UID mean something
else.

**It starts at now and never backfills.** A first run takes the current
UID as the mark, so the years of mail Bridge is indexing are not read.
Reading them would send a great many invitations for events long past.

### What it looks at, in order

1. **Already an invitation** — skipped entirely. Proton offers to add
   these itself, so acting on them would duplicate the mail client.
2. **schema.org JSON-LD** in the message's HTML. Deterministic, no
   inference, and the way bookings, tickets and reservations actually
   describe themselves. A reservation nests the event under
   `reservationFor`, so the search recurses. Travel is read by type:
   flights ("Flight BA117 LHR → JFK"), trains, coaches and ferries from
   departure to arrival, a stay from check-in to check-out ("Stay: …"), a
   table and a hire car — each with its booking reference. A stay given
   only as dates becomes an all-day event covering check-out day.
3. **Prose**, to `lab/background` at `ai.twolfe.dev` — the Studio's
   model while it is awake, the mini's otherwise (`ollama/README.md`,
   "Roles") — for the same kinds of entry: appointments, journeys and
   stays. Only what the first two could not answer reaches here. Every
   such email is read by a model: with none available the watcher stops
   short of it, and resumes from it when one answers — the health check goes red after 15 minutes stuck,
   which Gatus pages.

A model's reading is treated as weaker evidence than a sender's own
structured data: an answer it cannot parse whole is discarded, an event
dated before the mail announcing it is refused, and the invitation says
in its body that a model read it.

### What it sends

A **reply**, in the same conversation — `In-Reply-To` and `References`
carried from the source — so the "Add to calendar" click happens next to
the booking that caused it rather than in a message about nothing.

**One invitation per entry.** An itinerary is several — out and back,
each leg of a connection, the hotel between — and each gets its own
reply with its own UID: the message's ID and the entry's place in it, so
reading the same mail twice updates rather than duplicates, and a return
leg never overwrites the outbound. The first entry keeps the UID a
message's only event always had.

The calendar part declares `METHOD:PUBLISH`, with the same method on the
MIME part, which is what makes a client offer to add it rather than
treat it as a file. PUBLISH, not REQUEST, and no attendee: there is
nobody to reply to, and a recipient listed as attending an event they
also organise is what stops a client offering it. It is the one calendar
part — an `.ics` attachment of the same bytes used to ride along, and a
client that understood invitations then saw two and interpreted
neither.

**The recipient is configuration, never the message.** Nothing is read
off the source's `From`, `To` or `Reply-To`. A reply-shaped mail is one
field away from answering the airline, and there is a test whose only job
is to keep it that way.

## Reachability

No published ports and no route through the front door. Bridge binds
`127.0.0.1` inside the container and the image mirrors 143 and 25 onto
`0.0.0.0` with socat, so a container on the `lab` network reaches it at
`bridge:143` and `bridge:25` — and nothing outside that network reaches
it at all. The watcher will be the only client.

Nothing in Gatus watches this: there is no port to probe from the Pi, and
the watcher deliberately publishes none. What is visible is the container
— Beszel sees it, and bridge's healthcheck is the socat mirror answering
— which says a process is running rather than that mail is being read.
Closing that gap properly needs the watcher to answer a health request,
and it can wait until there is a reason to believe it is needed.

## Order of operations

The interactive login comes first and cannot be automated — it needs a
password, a second factor and a mailbox password. `RUNBOOK.md` has it.
Deploying before that login leaves a container that runs and serves
nothing.
