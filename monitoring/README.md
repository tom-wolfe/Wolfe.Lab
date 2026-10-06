# Monitoring

How the lab watches itself: OpenTelemetry for every signal, Grafana's
stack to read it, Alloy to collect it, and the layers outside Grafana that
watch Grafana. The services are each other's: `alloy/` collects,
`grafana/` stores and alerts, `gatus/` asks every service from the Pi,
`heartbeat/` proves the schedules are alive, and `beszel/` is the friendly
view of the machines. The rule that orders the layers — a watcher must not
share the fate of the thing it watches — is in the root README, "How
monitoring works".

## Why OpenTelemetry, Grafana and Alloy

**OpenTelemetry is the part that lasts.** The instrumentation outlives
whichever backend receives it. **Grafana's stack** over a single-binary
store such as OpenObserve: more to run (Loki for logs, Tempo for traces,
Prometheus for metrics, Grafana in front), but it is the standard shape,
the one that carries over to work, and its dashboards are the best on
offer. Every signal is kept **seven days**, with a size cap beside it as
the guard.

**Alloy rather than the upstream Collector.** Both speak OTLP, so the
applications do not care, and a switch would be the collectors'
configuration alone. What decides it is container logs on the Macs:
Docker Desktop keeps them inside its VM, out of reach of a collector
reading files on the host, and upstream has no receiver that reads them
through the Docker API, where Alloy does. Alloy also ships the Loki and
Prometheus pipelines natively, has a UI for debugging a pipeline, and is
what Grafana's own documentation assumes.

## The label schema

Dashboards and alert rules are written against labels, so they were
decided before anything was emitted, and every signal carries them:

- `service.name` — what emitted it (`mail-watcher`, `caddy`, `lab`).
- `host.name` — the node (`mini`, `studio`, `pi`).
- `lab.area`, `lab.service`, `lab.component` — where it lives in the
  repo: `monitoring`, `alloy`, `forwarder`.
- `lab.role` — `server` or `hybrid`, so a rule leaves the Studio out in
  one matcher: its Gatus check never alerts, and a silent node pages only
  when it is a server.

The lab's CLI holds the one list (`TelemetryAttribute`), and its checks
hold every collector's and store's configuration to it.

## The shape

- **A collector on every node, as a host process** (`alloy/`). On a Mac a
  container sees Docker Desktop's VM, not the Mac, and none of the host
  processes' logs. On the Studio it follows the hybrid rule: nothing waits
  on it, and what it buffers catches up when the Studio wakes.
- **Every node forwards to one gateway**, in Grafana's stack on the mini:
  one ingest point, one place to relabel or drop. It is reached over the
  tailnet directly, never through caddy — a broken front door must not
  hide the evidence of its own failure.
- **A component declares its own collection.** Its `metrics` facet says
  where it serves them and its deploy lists them in a targets file the
  collector reads, so no central list of targets exists to drift; every
  container's logs are collected without declaring.
- **Each node reports on itself and its containers**: host metrics on every
  node, container stats from Alloy's cAdvisor on Linux and from a cAdvisor
  container on a Mac — the one container given the Docker socket
  (`alloy/README.md`).
- **Applications trace themselves.** The mail watcher's logs, HTTP calls
  and model calls go out over OTLP; Ritten traces every run of the lab's
  CLI — the job, its steps, the commands and HTTP calls beneath them
  (`platform/lab/README.md`).
- **The backend on the mini's internal disk**, Grafana in front, behind
  caddy, tailnet-only. Telemetry is disposable and is not backed up;
  datasources, dashboards and alert rules are provisioned from the repo,
  so a rebuild loses history and nothing else.

## Alerting, and who watches Grafana

Grafana alerts — on the nodes' thresholds and a server gone quiet — to
Pushover, like everything else. Grafana shares the mini's fate, so the
layers outside it stay: Gatus on the Pi pages when the mini, or Grafana,
stops answering, and healthchecks.io stays the one observer outside the
building. Grafana can answer HTTP with its rule engine stalled, so one
rule always fires and pings healthchecks.io, which pages when the pings
stop (`grafana/README.md`).

**Beszel stays, as the view; Grafana alerts.** The collectors report what
Beszel's agents do, and its thresholds are Grafana's rules, so a problem
pages once, from rules in the repo. Beszel alerts on nothing, and is kept
for what it does better: a friendlier UI, and a phone app self-hosted
Grafana does not match.

## Why not Garage for Loki and Tempo

Object storage is the standard shape, but on one backend node it buys
nothing, and here it costs: the nightly `garage-backup` snapshots all of
Garage's blocks — no bucket can be left out — so a week of churning
telemetry would ride into restic and B2 every night, and its cold copy
stops Garage, and the stack with it. A second, unbacked Garage would fix
both at the price of a second Garage to run. Telemetry is disposable, so
moving Loki and Tempo to S3 later is configuration and at most seven days
of history; the time for it is when the backend moves to the Linux node.
