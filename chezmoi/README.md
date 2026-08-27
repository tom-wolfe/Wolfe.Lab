# chezmoi — the CD tick

The lab's game tick: `flows/update/flow.yaml` runs `chezmoi update` on the mini
every 15 minutes — pull the repo, converge machine config. It is the single
clock and the only place the repo gets pulled; every service's
`deploy-<svc>` flow chains on this flow reaching SUCCESS, so the order is
always pull → config → deploys, and a broken config converge *pauses*
deployment (the next green tick converges everything — the deploys are
idempotent no-ops when nothing changed).

The whole chezmoi story lives in this slice: `home/` is the *source* — the
declarative machine plane itself (dotfiles, Brewfile, install scripts, and
the `create_` secret-cache templates under `home/Docker/`; `.chezmoiroot`
points chezmoi at it) — and `flows/update/` is the job that runs it on the
server.

## The poke (not yet wired)

`flows/update/flow.yaml` carries a webhook trigger so a merge can deploy in
seconds instead of within the tick. Unwired by choice, not necessity — a
native Forgejo repo webhook on push-to-main could call it today
(declarable in `forgejo/tofu` via `forgejo_repository_webhook`), or
post-merge CI once the Actions runner exists:

```sh
curl -X POST "http://macmini.local:8180/api/v1/main/executions/webhook/lab/chezmoi-update/<key>"
```

The key sits in the flow YAML (committed — acceptable because the endpoint
is LAN-only and the key can only start this one predefined flow; rotate it
by editing the flow and re-applying). The webhook path is exempt from basic
auth in `kestra/application.yaml` precisely so CI needs no admin
credential.
