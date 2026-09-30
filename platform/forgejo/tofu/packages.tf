# The package token the publishes push with (platform/lab/RUNBOOK.md, "The package
# token").

resource "forgejo_repository_action_secret" "packages_token" {
  repository_id = forgejo_repository.wolfe_lab.id
  name          = "PACKAGES_TOKEN"
  data          = var.packages_token
}
