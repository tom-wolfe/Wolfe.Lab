# "The Poke"
# Sets up a webhook that pings a Kestra flow on every push to main.

locals {
  poke_flow        = yamldecode(file("${path.module}/../../kestra/flows/push-to-main/flow.yaml"))
  kestra_container = yamldecode(file("${path.module}/../../kestra/compose.yaml")).services.kestra.container_name

  poke_key = one([
    for t in local.poke_flow.triggers : t.key
    if t.type == "io.kestra.plugin.core.trigger.Webhook"
  ])

  # 8080 is kestra's container-internal port and `main` its tenant —
  # Kestra constants, not lab configuration.
  poke_url = "http://${local.kestra_container}:8080/api/v1/main/executions/webhook/${local.poke_flow.namespace}/${local.poke_flow.id}/${local.poke_key}"
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
