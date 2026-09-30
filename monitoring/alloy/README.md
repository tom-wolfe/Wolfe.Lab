# Alloy

The lab's collector (ROADMAP.md #11): Grafana Alloy, a host process on
every node, and the only thing that writes to the telemetry stores
(`monitoring/grafana`). The mini's today; the other nodes forward to it in
step 3.

| Concern | Handled by |
| --- | --- |
| Binary | the agent's `package` in `agent/ritten.json`: the release the deploy installs, pinned (platform/lab/README.md, "Packages and tools") |
| Process | `agent/`, the `agents` workflow: a launchd unit per node, `.forgejo/workflows/alloy-agent.yaml` |
| Config | `agent/config/config.alloy`, published as an artifact to `${LAB_ROOT}/alloy`; a change restarts the agent (platform/lab/README.md, "Artifacts") |
| State | `${LAB_DATA}/alloy` — its write-ahead log; disposable |
| UI | `127.0.0.1:12345` on the node — component health and a live view of each pipeline |

## Why a host process

On a Mac a container sees Docker Desktop's VM, not the Mac: its CPU,
memory and disks rather than `/Volumes/Data1`, and none of the host
processes' logs. So Alloy runs on the host, like the Beszel agent, and
reaches containers through the Docker socket.

## What it does today

- **Receives OTLP** on `127.0.0.1:4317` (gRPC) and `:4318` (HTTP). A
  container sends to `host.docker.internal`, which Docker Desktop forwards
  to the host's loopback; nothing listens beyond the node until other
  nodes forward here.
- **Labels it** with the node: `host.name` and `lab.role`, from the
  agent's environment, set only where the sender did not — an application
  that names its own host keeps it.
- **Reads every container's logs** through the Docker socket
  (`/var/run/docker.sock`; on a Mac they live inside the VM, out of reach
  as files), each stream named `service_name` for its container and
  carrying every `lab.*` label the deploy put on it — `lab.area`,
  `lab.service`, `lab.component` (platform/lab/README.md) — mapped as a
  set, so a new one needs no change here. A container that sends its own logs over OTLP is
  labelled `lab.logs: otlp` in its compose file and left out, rather than
  stored twice; the mail watcher is the one today. The first start reads
  each container's whole history — Loki refuses what is older than its
  seven days — and tails from then on.
- **Reads the node's host-process logs**: the files declared in
  `${LAB_ROOT}/.logs` — each agent deploy's for its agents (Alloy's own,
  Beszel's, ollama's), and chezmoi's for the runner on a Mac that has one
  — each stream named and placed by its target. A file is read from where
  it ended when first seen: a launchd log has been appended to for as long
  as its agent has existed, and that history is a burst, not a record. A
  new target is found within five minutes.
- **Labels every log stream it reads** with the node — `host_name` and
  `lab_role` — once, where they are written, rather than per source.
- **Forwards it** to the stores on their loopback ports: logs to Loki's
  `/otlp` (container logs to its push API), traces to Tempo, metrics to
  Prometheus's OTLP receiver.
- **Reports on itself**: its own metrics, under the same labels with
  `service_name="alloy"`, by remote write — so a pipeline that is failing
  shows up in Grafana beside what it carries.

The node's identity is the agent's (`LAB_HOST`, `LAB_ROLE` in
`agent/ritten.json`, and `LAB_ROOT` for where the targets are), so one
config serves any node it is placed on.

## Checking it

```
curl -s 127.0.0.1:12345/-/ready
tail ~/.cache/alloy/alloy.log
```

Every component should read *healthy* in the UI. In Grafana,
`alloy_build_info{host_name="mini"}` proves the self-report arrives.
