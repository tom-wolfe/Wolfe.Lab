# Alloy

The lab's collector (monitoring/README.md): Grafana Alloy, a **forwarder** on
every node as a host process, the mini included. Each gathers what its own
node has and forwards it to the **gateway**, the only thing that writes to
the telemetry stores — a container in Grafana's own stack
(`monitoring/grafana`), so it moves with them.

| Concern | Handled by |
| --- | --- |
| Binary | the agent's `package` in `forwarder/ritten.json`, per node: the release the deploy installs, pinned (platform/lab/README.md, "Packages and tools") |
| Process | `forwarder/`, the `agents` workflow: a launchd unit on a Mac, a systemd user unit on the Pi, `.forgejo/workflows/alloy-agent.yaml` |
| Config | `forwarder/config/`, published as an artifact to `${LAB_ROOT}/alloy`; a change restarts every node's agent (platform/lab/README.md, "Artifacts") |
| Gateway | the `gateway` component of `monitoring/grafana/compose`: its container and `config/alloy/gateway.alloy` |
| A Mac's containers | `cadvisor/`, a compose component on the mini, `.forgejo/workflows/alloy-cadvisor.yaml` (below) |
| State | `${LAB_DATA}/alloy` — its write-ahead log; disposable |
| UI | `127.0.0.1:12345` on each node — component health and a live view of each pipeline |

## Why a host process — and why the gateway is not one

On a Mac a container sees Docker Desktop's VM, not the Mac: its CPU,
memory and disks rather than `/Volumes/Data1`, and none of the host
processes' logs. So Alloy runs on the host, like the Beszel agent, and
reaches containers through the Docker socket. The gateway needs none of
that: it only receives and writes, so it is a container beside the stores,
reaching them by name on their `telemetry` network.

## Why a gateway

Every node needs a collector of its own, because most of what it reads is
only reachable there: the Docker socket, the journal, launchd's log files.
Where it all goes is a separate choice, and the lab sends it through one
collector rather than letting each node write to the stores:

- **The stores publish nothing.** None of Loki, Tempo or Prometheus has
  auth; the gateway's three ports, which only accept writes, are all the
  network sees of them.
- **One place to relabel, drop or move.** When the backend moves to the
  Linux node, the gateway moves with it, as part of its stack, and every
  node follows one name (`LAB_GATEWAY`).
- **One forwarder, identical everywhere.** The mini forwards to the
  gateway as every other node does, so no node's collector is special.

## The config

Two files for the forwarder, and one for the gateway:

- **`node.alloy`** — what every node gathers from itself, as one
  component (`collect`) that its root places. Everything it reads it
  labels with the node, here and nowhere else.
- **`forwarder.alloy`** — every node's root: the node's own, to the
  gateway.
- **`gateway.alloy`**, in Grafana's stack — what the nodes forward, to the
  stores. It gathers nothing of its own.

The gateway passes what it receives through as it arrived. Each node has
already labelled its own; a label the gateway set would say nothing about
where anything came from.

The node's identity is the agent's — `LAB_HOST`, `LAB_ROLE`, `LAB_ROOT`
for where the targets are, `LAB_GATEWAY`, and `DOCKER_HOST` where the
Docker socket is not the standard one, all in `forwarder/ritten.json` —
so one config serves every node.

## What every node does

- **Receives OTLP** from its applications on `127.0.0.1:4317` (gRPC) and
  `:4318` (HTTP). A container sends to `host.docker.internal`, which
  Docker Desktop forwards to the host's loopback.
- **Labels it** with the node: `host.name` and `lab.role`, set only where
  the sender did not — an application that names its own host keeps it.
- **Reads every container's logs** through the Docker socket
  (`/var/run/docker.sock` unless the agent sets `DOCKER_HOST`; on a Mac
  they live inside the VM, out of reach as files), each stream named
  `service_name` for its container and carrying every `lab.*` label the
  deploy put on it — `lab.area`, `lab.service`, `lab.component`
  (platform/lab/README.md) — mapped as a set, so a new one needs no change
  here. A container that sends its own logs over OTLP declares
  `logs: otlp` on its component, which the deploy labels it with, and is
  left out rather than stored twice; the mail watcher is the one today. The first start reads each
  container's whole history — Loki refuses what is older than its seven
  days — and tails from then on. Containers are found once a minute, so
  one that comes and goes between two looks is never read.
- **Reads the node's host-process logs from files**: those declared in
  `${LAB_ROOT}/.logs` — each agent deploy's for its agents (Alloy's own,
  Beszel's, ollama's), and chezmoi's for the runner on a Mac that has one
  — each stream named and placed by its target. A file is read from where
  it ended when first seen: a launchd log has been appended to for as long
  as its agent has existed, and that history is a burst, not a record. A
  new target is found within five minutes.
- **Reads the journal**, on a Linux node: the system's and its agents'
  alike, each stream named for its unit (a user unit by its own name, a
  lab agent's as on a Mac: `alloy`, not `dev.twolfe.alloy`), or
  for what it logged as where it has none. A no-op on a Mac.
- **Labels every log stream it reads** with the node — `host_name` and
  `lab_role` — once, where they are read, rather than per source.
- **Scrapes the metrics the lab declares**, every 30 seconds: each
  endpoint a deploy lists in `${LAB_ROOT}/.metrics`, a file per component
  naming its address, path and where it lives, as `.logs` does for log
  files (platform/lab/README.md, "Declarations").
- **Reports on the node itself**: CPU, memory, disks, network and, where
  the node has sensors, temperatures (Alloy's own node exporter) — what
  only a host process sees, since a container on a Mac sees Docker
  Desktop's VM. A Mac's system volumes are left out (they share the data
  volume's space), as are interfaces that only carry what a real one
  does: loopback, tunnels, bridges.
- **Reports each container's resources**: its CPU, memory, network and
  OOM kills, named `service_name` for the container and placed by its
  deploy's `lab.*` labels, as its logs are. On a Linux node Alloy reads
  them from the node's cgroups itself (its cAdvisor), naming containers
  through Docker's and containerd's sockets. On a Mac the cgroups are
  inside Docker Desktop's VM: there a cAdvisor container reads them, and
  its series arrive through `.metrics` and are named the same way.
- **Reports on itself**: its own metrics, under the same labels with
  `service_name="alloy"` — so a pipeline that is failing shows up in
  Grafana beside what it carries.

## The Docker socket

The socket is Docker's control plane: anything that can talk to it can
start a container that mounts the whole disk, which is root by another
route. So the rule is that **no container gets the socket**: it is what
keeps one compromised container from being a compromised host, and the
containerised runner never mounts it into a job for the same reason. A
host process is a different shape: the
forwarder runs as the login user, who owns the socket and can run
`docker` at any prompt, so reading it grants nothing new. On Linux the
same holds for containerd's socket, which the `docker` group is given at
bootstrap (RUNBOOK.md, "Secondary node bootstrap").

**The one exception is `cadvisor/`, on a Mac.** Docker Desktop keeps the
cgroups inside its VM, where no host process reaches, and the mini keeps
containers for good, so a container reads them — and names them through
both sockets, read-only, though read-only does not limit what a socket
grants. It is not privileged (`SYSLOG` and `/dev/kmsg` alone, for OOM
kills), it publishes on loopback, and its image is pinned. A Linux node
never runs it.

## What the gateway does

- **Receives from every node's forwarder**, the mini's included, on ports
  its container publishes on every interface of the mini — the LAN's and
  the tailnet's, like the mini's other services. A node reaches them over
  the tailnet, never through caddy: a broken front door must not hide the
  evidence of its own failure.

  | Port | What | From a forwarder's |
  | --- | --- | --- |
  | `:4417` | OTLP gRPC | applications' telemetry |
  | `:4418` | Loki push | container, file and journal logs |
  | `:4419` | Prometheus remote write | its own metrics and its containers' |

  Logs keep the time they were read, not the time they arrived, so a
  node catching up after an outage lands where it happened.
- **Writes to the stores** by name on the `telemetry` network: OTLP logs
  to Loki's `/otlp`, traces to Tempo, metrics to Prometheus's OTLP
  receiver; the read logs to Loki's push API and Alloy's metrics by remote
  write. Its write-ahead log is `~/Docker/grafana/alloy`, as disposable as
  a forwarder's.

## Checking it

On any node:

```
curl -s 127.0.0.1:12345/-/ready
tail ~/.cache/alloy/alloy.log            # a Mac
journalctl --user -u dev.twolfe.alloy -n 50   # the Pi
```

The gateway, on the mini:

```
docker logs alloy-gateway --tail 50
curl -s 127.0.0.1:12346/-/ready          # the gateway's UI and metrics, published on the mini's loopback
```
