# ollama

The lab's model endpoint: a host process on the mini and another on the
Studio, behind the front door at `ai.twolfe.dev`, and the models they
serve, declared by what each is used for.

## Why this service has no compose file

A container on macOS gets no access to the Apple GPU, so a containerised
model runs on CPU and is uselessly slow for anything but the smallest
work. The model server is therefore the one thing in the lab that has to
be a host process, and this service is the first of a second kind: a service
whose stack is a supervised agent rather than a container.

What that means concretely — each server, `mini/` and `studio/`, is an
`agent` component rather than a compose stack: `lab deploy` renders it to
a launchd unit and bootstraps it. The declaration
is platform-neutral, so when the primary node stops being a Mac this
service does not change; only which renderer the CLI registers does.

It still contributes a `caddy.caddyfile` like any other service, which the
caddy deploy gathers from the repo.

## Reachability, and the honest cost of it

`OLLAMA_HOST` binds `0.0.0.0:11434` rather than loopback, and it has to:
caddy runs in a container, a container reaches a host process through
`host.docker.internal`, and that address does not resolve to the host's
loopback. Loopback would make the endpoint unreachable from the one
thing meant to consume it.

The cost is that the port is then open to the house LAN, and **ollama has
no authentication of any kind** — anyone on the wifi can list the models
and spend the mini's CPU. Two things narrow it and neither closes it:
the front-door route claims only the tailnet name, so the *proxied* path
is tailnet-only; and the LAN is the house's own.

If that stops being acceptable, the move is to bind the mini's Tailscale
address instead of `0.0.0.0`. Containers reach it by egressing through
the host, so caddy still works, and the LAN stops being able to. The cost
is a machine-specific address in the server's declaration and an endpoint
that dies with the tailnet.

## What depends on it, and how it degrades

Nothing may depend on it. It is the optional tier of the mail event
scanner: an email with an attached invitation or structured data is
handled without a model at all, and only prose needs one. If this service
is down, the watcher stops at the first email that needs a model and
reads on from it when the model is back; nothing is skipped, and a stall
past 15 minutes pages through the watcher's health check.

Paperless's AI features are the other consumer, and degrade the same
way: with no model, its suggestions and chat fail and its own
classifier-based suggestions carry on (`personal/paperless/README.md`, "AI").

The Studio (`README.md`, "Nodes") is a second upstream above the mini — the same
rule one level up: the Studio may serve, but nothing may depend on it.

## The Studio

A second server, `studio/`, the same agent on the Studio's runner
(`ollama-studio.yaml`): `dev.twolfe.ollama`, serving what the Studio can
run. The route asks it first — `lb_policy first`, health checked every ten
seconds — and falls through to the mini the moment it does not answer,
which is every evening the Studio sleeps. A request that lands in the gap
before the check notices is retried on the mini (`lb_try_duration`).
Callers see one endpoint whichever machine served.

It runs `qwen3.6:35b-a3b`, a mixture of experts: about 24 GB resident, but
only ~3B parameters active per token, so much better than the mini's 9B at
a fraction of a dense model's compute. That is the budget a job has on
somebody's workstation, as is `OLLAMA_NUM_PARALLEL=1`: one request at a
time. Models unload after ollama's five idle minutes.

What it does not need: the mini's drive, and so the mini's file-access
grant. Its store is the default `~/.ollama/models` on the internal disk.
It binds `0.0.0.0` like the mini's, for the same reason and with the same
cost (above), and servers reach it on 11434 alone
(`network/tailscale/tofu/policy.hujson`). Gatus asks it directly, without alerting:
off is its normal state, and the check that pages is the front door's,
which the mini keeps up.

## Uses

Callers ask for a **use**, not a model: `lab/interactive`, `lab/background`,
`lab/embedding`. Each is a component of its own, in its own directory, saying
what it is for and what each server runs for it — a default model and
context, and a server's own where it differs:

```yaml
# interactive/component.yaml
name: interactive
kind: model
workflow: ollama
model: "qwen3.6:35b-a3b"
context: 16384
servedBy:
  studio: {}
  mini: { model: "qwen3.5:9b", context: 8192 }
```

A deploy makes `lab/<use>` on each server from that server's model, with its
context (`ollama create`: a manifest that shares the weights, no extra disk),
so a caller gets the best model of whichever machine answered — the Studio's
by day, the mini's while it sleeps — without knowing which, and without a
fallback of its own.

| Use | For | Studio | Mini |
|---|---|---|---|
| `interactive` | A person waiting: Paperless's suggestions and chat, Grafana's summaries | `qwen3.6:35b-a3b`, 16k | `qwen3.5:9b`, 8k |
| `background` | Unattended jobs: the mail scanner | `qwen3.6:35b-a3b`, 16k | `qwen3.5:9b`, 8k |
| `embedding` | Vectors for search: Paperless's index | `embeddinggemma:300m`, 2k | `embeddinggemma:300m`, 2k |

**Whether a model thinks is the caller's to say, not the use's.** Both Qwen
models think by default and stop when a request says so: Paperless asks
over the OpenAI-compatible `/v1` with `reasoning_effort: none`
(`personal/paperless/README.md`, "AI"), and Grafana's summaries think. So
one model serves both `interactive` and `background`, where it once took a
thinking model and its instruct twin, 37 GB on a morning that ran both.

**The context is the use's, per server**, because Ollama's OpenAI endpoint
takes none from the caller: a request over `/v1` gets the model's own, which
on the Studio would otherwise be whatever the machine's memory allows. A use
that wants a long window — the vault, one day — is a use of its own with
its own context, on the same model. Two uses of one model with different
contexts are two runners in memory, so a server's should share one where it
can, as the mini's do.

The catalog holds the rules, so a check catches them on the pull request:

- **Every server serves every use.** A use only the Studio served would be
  "not found" every evening, where the point is that it degrades to the
  mini's best.
- **Every server has a model for it**: its own, or the default.

`embedding` names its model once, as the default, and no server overrides
it, because vectors from two models are not comparable: an index built at
night on the mini and searched by day on the Studio would return nonsense
without failing. Nothing but that declaration holds it so, and changing the
embedding model means every consumer rebuilds its index.

A deploy also retires the name of a use no longer declared, so a caller
asking for it hears "not found" rather than whatever it used to mean.
Pulled models are never removed.

## Models

The mini's live on Data2, not where ollama would put them. The mini has a
256 GB internal disk with about 60 GB free, and a single useful model is
4–10 GB of that — so the default `~/.ollama/models` would put the lab one
careless `ollama pull` away from a full boot drive, which macOS handles
badly. The server's `OLLAMA_MODELS` points at `/Volumes/Data2/ollama/models`
and it requires the volume, so its deploy refuses to run while the drive is
unmounted rather than converging onto a shadow path.

They are not backed up: a model is a re-pullable artefact, not state.

**The gap that guard does not close.** It runs at converge time, and the
agent also starts at boot — where launchd may well get there before the
drive is mounted, the same race Docker loses. ollama would then find an
empty directory on the internal disk and serve no models at all. That is
why the Gatus check asserts a model is *listed* rather than that the
server answered: a 200 with an empty list is precisely this, and looks
healthy to anything shallower. If it turns out to happen often rather
than theoretically, launchd's `StartOnMount` is the fix.

The model is left to unload when idle, which is ollama's default. That
costs a cold load on the first request after a quiet spell — seconds to
tens of seconds — which is the thing to change first if the scanner feels
slow. `OLLAMA_KEEP_ALIVE=-1` in a server's `environment` pins it resident
at the cost of holding the RAM.

Which model serves a use is this service's decision; which use to ask
for is the caller's.

## Order of operations

Each server deploys as an agent, installing its pinned release
(`package`; platform/lab/README.md, "Packages and tools") before it
converges the unit. The uses deploy on their own (`ollama-models.yaml`),
on every node a server runs on, and again whenever anything under
`ai/ollama/` changes: each installs its node's server's release, puts it
first on the job's path — so the `ollama pull` and `ollama create` it runs
are the server's own version — and waits for the server to answer before
it pulls. Nothing needs to land before it.
