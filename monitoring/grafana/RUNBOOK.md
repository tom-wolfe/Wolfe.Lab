# Grafana — runbook

## First deploy

1. The vault items exist: `grafana-admin` (username, password),
   `pushover`, `healthchecks-ping-key`.
2. `heartbeat tofu` has applied, so `lab-grafana-watchdog` exists — until
   it does, every watchdog ping is a 404 and nothing pages for it.
3. Merge; `grafana compose` deploys on the mini. Docker creates the data
   directories under `~/Docker/grafana/` on first start.
4. Sign in at `https://grafana.twolfe.dev` as the admin. **Alerting →
   Alert rules**: Watchdog is *Firing* — that is healthy. Within five
   minutes, `lab-grafana-watchdog` on healthchecks.io goes green.

## The admin password

Grafana reads `GF_SECURITY_ADMIN_PASSWORD` only when it creates its
database, so a rotated password does not reach it by redeploying. After
changing `grafana-admin` in the vault:

```
ssh macmini "docker exec grafana grafana cli admin reset-admin-password '<new password>'"
```

## Starting the history over

Telemetry is disposable. To drop it all — a store's format changed, or
the disk is wanted back:

```
ssh macmini "docker compose --project-directory .local/share/Wolfe.Lab/grafana-server down"
ssh macmini "rm -rf ~/Docker/grafana/{loki,tempo,prometheus}"
```

then re-run `grafana compose`. Leave `~/Docker/grafana/grafana` alone
unless Grafana itself should start over: it holds users and preferences,
though nothing provisioned.
