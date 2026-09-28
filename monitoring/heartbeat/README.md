# heartbeat

The dead man's switch. Every alert path in this lab runs on the mini's
Actions runner and dies with it, or with Forgejo, or with the mini, so
the one signal that can report "the lab is off" has to come from
outside: `lab ping` sends healthchecks.io a ping every 15 minutes
(`.forgejo/workflows/heartbeat.yaml`), and healthchecks.io shouts when
the pings stop. It is the only observer outside the building.

| What | Where |
| --- | --- |
| The ping | `ritten.json` names the check and the project ping key; the job is one HTTP call |
| The check | `tofu/checks.tf` — its schedule, grace period, description and notification channels, declared rather than clicked |
| The workflow | every 15 minutes from the mini's runner, with no concurrency group, so it never queues behind a long job |

**Ping by slug, not UUID.** The URL is
`https://hc-ping.com/<ping-key>/<slug>`, from a project-wide ping key. So
it survives the check being destroyed and recreated, one vault item
covers every check the lab will ever have, and the slug sits beside what
it monitors. healthchecks.io derives the slug from the check's `name`,
which is why the name is written slug-shaped; confirm they match after
an apply. The slug is historical, and renaming it would recreate the
check.

**Two 1Password items, not interchangeable.** `healthchecks-api-key` is
the read-write *management* key, used only from `tofu/`;
`healthchecks-ping-key` is the far less privileged *ping* key the job
reads at run time through the runner's service account. Never put the
management key where a job can read it.

**It notifies Pushover and email.** Pushover is the one that reaches a
phone, landing beside the in-lab alerts, while this alert's *origin*
stays outside the lab, which is the whole point of it. Email is the
backstop for the case where Pushover is the thing that is broken.
Channels are configured in the healthchecks.io UI and only *referenced*
here, so a data source for a channel that has not been set up fails the
plan.

**Every watched job carries the same section.** A scheduled job that
must not silently stop pings its own check as its last step, declared
under `heartbeat` in its component's `ritten.json` the way
`restic/repositories/` does; the check itself goes in that slice's `tofu/`. None of them collect in
a shared root: a check belongs beside the thing it watches, and this
slice's is the scheduler itself.

The root's state key predates the slice and still reads `chezmoi/`;
renaming it is a state migration, not a rename, and buys nothing.
