# Grafana

The lab's telemetry backend (monitoring/README.md): **Loki** for logs, **Tempo**
for traces, **Prometheus** for metrics, and **Grafana** in front, at
`grafana.twolfe.dev` — on the tailnet, like every name under the wildcard —
with the **Alloy gateway** that writes to them (monitoring/alloy/README.md).
Two compose stacks on the mini: the stores and Grafana in `compose/`, installed
as `grafana-server`, and the gateway in `gateway/`, as `grafana-gateway`.

| Concern | Handled by |
| --- | --- |
| Containers | `.forgejo/workflows/grafana-compose.yaml` and `grafana-gateway.yaml`, on every push that touches their directory; the mini's host runner |
| Secrets | `compose/secrets.env`: the admin login (`grafana-admin`), Pushover (`pushover`, shared with Gatus) and the healthchecks.io ping key (`healthchecks-ping-key`, shared with the heartbeat) |
| Datasources, contact points, alert rules, dashboards | provisioned from `compose/config/grafana/provisioning/` — code, not state |
| The LLM app | installed at start (`GF_PLUGINS_PREINSTALL_SYNC`, pinned; Renovate bumps it) and configured from `provisioning/plugins/llm.yaml` (below) |
| The watchdog's check | `monitoring/heartbeat/tofu` (`lab-grafana-watchdog`) |
| Liveness | Gatus, from the Pi (`monitoring/gatus/compose/config/lab.yaml`) |
| Backup | none, on purpose (below) |

## What goes in

Only the gateway writes to the stores, by name on the stores' `telemetry`
network, which its own stack joins; none of them publishes a port. Every node's forwarder, the mini's
included, sends to the gateway's `:4417`–`:4419` over the tailnet, never
through caddy — a broken front door must not hide the evidence of its own
failure.

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
the internal disk, and the gateway's write-ahead log in `~/Docker/grafana/alloy`.

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

The **nodes** rules are Beszel's thresholds, on every node — CPU busy,
memory or a disk over 90% for ten minutes — and a server whose collector
has gone quiet for ten. The **Nodes** dashboard shows the same: each
machine, and each container on it. Both are provisioned, so a
change is a change to their files; the UI refuses an edit.

## AI

Grafana's LLM app (`grafana-llm-app`) is what its AI buttons ask: titles
and descriptions for a dashboard or a panel, a summary of a dashboard's
changes on save. It answers with the lab's own models rather than a
vendor's — `ai.twolfe.dev`, as a "custom" OpenAI-compatible provider with
no key, and both of the app's sizes (`base` and `large`) mapped to the
`lab/interactive` use (`ai/ollama/README.md`, "Uses"): someone is waiting
on the answer. So it is the Studio's model by day and the mini's smaller
one while the Studio sleeps. The app cannot ask a model not to think, so
its summaries think first: slower, and better for it. The self-hosted edition gets the app,
not Grafana Cloud's Assistant — the chat that writes queries — so this is
the modest end of it, had because it can be.

A plugin can only be installed, not provisioned, so the version is pinned
in `compose.yaml` and the settings are provisioning: redeploying restores
both. What the app sends a model is what a feature asks about — a
dashboard's JSON, a panel's query — and it goes no further than the
lab's own models.

## Memory

Every container has a limit — Grafana 768 MB, Loki and Tempo 512 MB,
Prometheus 1 GB — because the mini is already short of free pages when ollama's model
is loaded, and a telemetry store must not be what pushes a service out.
Grafana's was 512 MB until the LLM app: it sat at about 315 MB, and the
app's backend adds roughly 75 more, which left too little headroom for
alert rules and dashboards still to come.

## Runbook

First deploy, the admin password, and starting the history over are in
`RUNBOOK.md`.
