# The poke: push-to-main POSTs to the tick's webhook trigger, so a merge
# converges in seconds instead of waiting out the 15-minute tick — which
# stays, as the reconciliation loop and the poke's safety net.
#
# Nothing here is hand-copied from another slice — in a monorepo, the
# filesystem is the stack reference. The tick's flow file is the source
# of truth for the webhook path (its namespace/id) and key (its webhook
# trigger); kestra's compose file is the source of truth for the
# container name. Rotating the key or renaming the tick is one edit over
# THERE: lab.forgejo/plan goes WARNING with this webhook's update in the
# diff, and the apply re-points it. If the tick ever loses its webhook
# trigger, one() fails the plan — loudly, before anything is touched.
#
# INTERIM, though, and named as such: this is still a consumer's guess
# at a URL whose format belongs to the kestra slice. The config plane
# (ROADMAP item 8) replaces this derivation with kestra PUBLISHING the
# URL to Garage when the lab's second cross-slice value appears.
#
# Forgejo-to-Kestra is container-to-container on the `lab` network
# (never the public names — the offline rule from caddy/README.md). The
# KEY is the auth on this path: the endpoint is basic-auth-exempt, and
# the key can start that one flow and nothing else — the LAN-only threat
# model that lets it sit committed in the flow file. No HMAC secret:
# Kestra wouldn't verify it.
#
# No mirror race: the mini's checkout pulls the PRIMARY, anonymously
# over loopback HTTP (http://localhost:3000/..., the repo is public —
# primary.tf). A poked tick therefore sees the very push that fired this
# webhook. Consequence, accepted eyes-open: forgejo down means the tick
# can't pull and goes red — visible and alerting, per the
# make-failure-visible principle, and the deploy flows still run
# manually to resurrect it. Fresh machines still bootstrap from the
# GitHub mirror (setup.sh), then repoint.

locals {
  tick             = yamldecode(file("${path.module}/../../chezmoi/flows/update/flow.yaml"))
  kestra_container = yamldecode(file("${path.module}/../../kestra/compose.yaml")).services.kestra.container_name

  poke_key = one([
    for t in local.tick.triggers : t.key
    if t.type == "io.kestra.plugin.core.trigger.Webhook"
  ])

  # 8080 is kestra's container-internal port and `main` its tenant —
  # Kestra constants, not lab configuration.
  poke_url = "http://${local.kestra_container}:8080/api/v1/main/executions/webhook/${local.tick.namespace}/${local.tick.id}/${local.poke_key}"
}

resource "forgejo_repository_webhook" "kestra_poke" {
  repository_id = forgejo_repository.wolfe_lab.id
  type          = "forgejo"
  active        = true
  branch_filter = "main"
  events        = ["push"]

  config = {
    content_type = "json"
    url          = local.poke_url
  }
}
