# ollama

The lab's model endpoint: one host process on the mini, behind the front
door at `ai.twolfe.dev`.

## Why this slice has no compose file

A container on macOS gets no access to the Apple GPU, so a containerised
model runs on CPU and is uselessly slow for anything but the smallest
work. The model server is therefore the one thing in the lab that has to
be a host process, and this slice is the first of a second kind: a slice
whose stack is a supervised agent rather than a container.

What that means concretely — `ritten.json` declares an `agents` section
instead of naming a compose file, `lab deploy` renders it to a launchd
unit and bootstraps it, and there is nothing to install into the release
directory because nothing on the node reads from there. The declaration
is platform-neutral, so when the primary node stops being a Mac this
slice does not change; only which renderer the CLI registers does.

It still contributes a `caddy.caddyfile` like any other slice, which the
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
is a machine-specific address in `ritten.json` and an endpoint that dies
with the tailnet.

## What depends on it, and how it degrades

Nothing may depend on it. It is the optional tier of the mail event
scanner: an email with an attached invitation or structured data is
handled without a model at all, and only prose needs one. If this slice
is down, the watcher stops at the first email that needs a model and
reads on from it when the model is back; nothing is skipped, and a stall
past 15 minutes pages through the watcher's health check.

Paperless's AI features are the other consumer, and degrade the same
way: with no model, its suggestions and chat fail and its own
classifier-based suggestions carry on (`paperless/README.md`, "AI").

The Studio (ROADMAP #7) is a second upstream above the mini — the same
rule one level up: the Studio may serve, but nothing may depend on it.

## The Studio

A second component, `studio/`, the same `ollama` shape on the Studio's
runner (`ollama-studio.yaml`): the agent, `dev.twolfe.ollama`, and the
models it holds. The route asks it first — `lb_policy first`, health
checked every ten seconds — and falls through to the mini the moment it
does not answer, which is every evening the Studio sleeps. A request
that lands in the gap before the check notices is retried on the mini
(`lb_try_duration`). Callers see one endpoint whichever machine served.

**The Studio holds every model the mini does, and more.** While the Studio
is awake every request goes to it, so a model only the mini held would be
"not found" exactly when the Studio is on. So `studio/ritten.json` pulls
the mini's list, plus what only the Studio can run — today `qwen3:30b-a3b`:
a mixture of experts, about 18 GB resident but only ~3B parameters active
per token, so much better than the mini's 8B at a fraction of a dense
model's compute. That is the budget a job has on somebody's workstation, as
is `OLLAMA_NUM_PARALLEL=1`: one request at a time. Models unload after
ollama's five idle minutes.

What it does not need: the mini's drive, and so the mini's file-access
grant. Its store is the default `~/.ollama/models` on the internal disk.
It binds `0.0.0.0` like the mini's, for the same reason and with the same
cost (above), and servers reach it on 11434 alone
(`tailscale/tofu/policy.hujson`). Gatus asks it directly, without alerting:
off is its normal state, and the check that pages is the front door's,
which the mini keeps up.

## Roles

Callers ask for a **role**, not a model: `lab/background`, `lab/interactive`,
`lab/embedding`. Each server's `ritten.json` declares which of its pulled
models fills each role, and a deploy points the `lab/<role>` alias at it
(`ollama cp`: a manifest, no extra disk). A caller then gets the best model
of whichever machine answered — the Studio's by day, the mini's while it
sleeps — without knowing which, and without a fallback of its own.

| Role | For | Studio | Mini |
|---|---|---|---|
| `background` | Unattended jobs: the mail scanner | `qwen3:30b-a3b` | `qwen3:8b` |
| `interactive` | A person waiting: Paperless's suggestions and chat | `qwen3:30b-a3b` | `qwen3:8b` |
| `embedding` | Vectors for search: Paperless's index | `embeddinggemma:300m` | `embeddinggemma:300m` |

`background` and `interactive` are the same model on the Studio for now.
They part when the Obsidian front end (ROADMAP #7, step 6) picks a bigger
interactive model; that is the moment to weigh its memory against the
desk.

The `check` job holds two rules, reading both components' files so a pull
request that changes either is caught:

- **Every server declares every role.** A role only the Studio declared
  would be "not found" every evening, where the point is that it degrades
  to the mini's best. There is no strict, Studio-only role.
- **A role marked `identical` names the same model everywhere.** That is
  `embedding`: vectors from two models are not comparable, and an index
  built at night on the mini and searched by day on the Studio would
  return nonsense without failing. Changing the embedding model means
  every consumer rebuilds its index.

A deploy also removes the alias of a role no longer declared, so a caller
asking for it hears "not found" rather than whatever it used to mean.
Pulled models are never removed.

## Models

They live on Data2, not where ollama would put them. The mini has a
256 GB internal disk with about 60 GB free, and a single useful model is
4–10 GB of that — so the default `~/.ollama/models` would put the lab one
careless `ollama pull` away from a full boot drive, which macOS handles
badly. `OLLAMA_MODELS` points at `/Volumes/Data2/ollama/models` and the
slice declares the volume, so `lab deploy` refuses to run while the
drive is unmounted rather than converging onto a shadow path.

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
slow. `OLLAMA_KEEP_ALIVE=-1` in the `environment` block pins it resident
at the cost of holding the RAM.

Which model fills a role is this slice's decision; which role to ask
for is the caller's.

## Order of operations

`brew "ollama"` is in the mini's chezmoi profile, so the binary arrives
with a chezmoi apply. `lab deploy` refuses an agent whose program is
not on the node rather than installing a unit that could only fail, so
on a first run the chezmoi job has to land before this one. Both trigger
off a push; if this one goes first, re-run it.
