# Grafana

The lab's telemetry backend (ROADMAP.md #11): **Loki** for logs, **Tempo**
for traces, **Prometheus** for metrics, and **Grafana** in front, at
`grafana.twolfe.dev` — on the tailnet, like every name under the wildcard.
One compose stack on the mini, release `grafana`.

| Concern | Handled by |
| --- | --- |
| Container | `.forgejo/workflows/grafana-compose.yaml`, on every push that touches `compose/`; the mini's host runner |
| Secrets | `compose/secrets.env`: the admin login (`grafana-admin`), Pushover (`pushover`, shared with Gatus) and the healthchecks.io ping key (`healthchecks-ping-key`, shared with the heartbeat) |
| Datasources, contact points, alert rules | provisioned from `compose/config/grafana/provisioning/` — code, not state |
| The watchdog's check | `monitoring/heartbeat/tofu` (`lab-grafana-watchdog`) |
| Liveness | Gatus, from the Pi (`monitoring/gatus/compose/config/lab.yaml`) |
| Backup | none, on purpose (below) |

## What goes in

Only the mini's Alloy writes to the stores, from the host: Loki's `:3100`,
Prometheus's `:9090` and Tempo's OTLP on `:14317`/`:14318` are published on
loopback alone. (`:4317` and `:4318` on the host are Alloy's own, where
applications send.) Every other node forwards through that Alloy, over the
tailnet, never through caddy — a broken front door must not hide the
evidence of its own failure.

All three take OTLP: logs into Loki's `/otlp`, traces into Tempo, metrics
into Prometheus's OTLP receiver (and scraped metrics by remote write).
Prometheus promotes the lab's label schema — `service.name`, `host.name`,
`lab.area`, `lab.service`, `lab.component`, `lab.role` — from resource
attributes onto every series, so a metric filters the way its logs and
traces do. The datasources link both ways: a span to its logs, a log
line to its trace.

## Seven days, and no backup

Every store keeps seven days: Loki's compactor, Tempo's block retention
(set for both its scheduler and its workers — Tempo 3 splits them),
Prometheus's `--storage.tsdb.retention.time`, with an 8 GB size cap beside
it. Telemetry is disposable and is not backed up; everything worth keeping
is provisioned from the repository, so a rebuild loses history and nothing
else. Data lives in `~/Docker/grafana/{grafana,loki,tempo,prometheus}` on
the internal disk.

**Why not Garage.** The standard shape keeps Loki and Tempo in object
storage, but on one backend node it buys nothing and costs two things:
`garage-backup` snapshots all of Garage's blocks, so a week of churning
telemetry would ride into restic and B2 every night, and its 02:30 cold
copy stops Garage, and this stack with it. Moving to S3 is configuration
and at most seven days of history; the time is when the backend moves to
the Linux node.

## Alerts, and who watches Grafana

Alert rules go to Pushover, like everything else. Grafana shares the
mini's fate, so the layers outside it stay (README.md, "How monitoring
works"): Gatus, on the Pi, pages when `grafana.twolfe.dev` stops answering.

But Grafana can answer HTTP with its alerting stalled, and a dead alerting
engine looks exactly like a quiet night. So one rule, **Watchdog**, always
fires. Its route re-sends it every five minutes to a webhook that pings
healthchecks.io (`lab-grafana-watchdog`), which pages when the pings stop:
proof that rules evaluate, Prometheus answers and notifications leave. A
firing Watchdog in Grafana's alert list is the healthy state.

## Memory

Every container has a limit — Grafana, Loki and Tempo 512 MB, Prometheus
1 GB — because the mini is already short of free pages when ollama's model
is loaded, and a telemetry store must not be what pushes a service out.

## Runbook

First deploy, the admin password, and starting the history over are in
`RUNBOOK.md`.
