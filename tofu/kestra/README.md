# kestra

Declaratively manages every Kestra flow. State lives in Garage
(`s3://tofu-state/kestra/terraform.tfstate`).

**Adding a job** = one commit: a YAML file in `flows/` (+ its host-side
script in `jobs/` if it touches the mini) — `flows.tf` picks up the file
automatically, and lab-job resolves the script from the repo checkout. Flows edited in the Kestra UI will be
reverted by the next apply; the repo is the source of truth.

## Usage

```sh
op run --env-file=secrets.env -- tofu init
op run --env-file=secrets.env -- tofu plan
op run --env-file=secrets.env -- tofu apply
```

Requires the `Kestra Admin` 1Password item — you create it before the
compose/kestra bootstrap (see that README's setup steps).

## The poke

`chezmoi-update` carries a webhook trigger so post-merge CI can deploy
immediately instead of waiting for the hourly schedule:

```sh
curl -X POST "http://macmini.local:8180/api/v1/main/executions/webhook/lab/chezmoi-update/<key>"
```

The key sits in the flow YAML (committed — acceptable because the endpoint
is LAN-only and the key can only start this one predefined flow; rotate it
by editing the flow and re-applying). The webhook path is exempt from basic
auth in `compose/kestra/application.yaml` precisely so CI needs no admin
credential.

## First-apply verification

Watch the first run of each flow in the UI (Executions → lab). Two things
were designed from documentation and want a live check:

- `ob sync` without `--continuous` is assumed to do a single pass and exit —
  confirm the execution completes rather than running forever (if it hangs,
  the CLI needs a one-shot flag added in `lab-job`).
- The SSH task is expected to accept the host key on first connect. If it
  instead fails on host-key verification, the task supports the usual
  known-hosts properties — add them then rather than pre-emptively.
