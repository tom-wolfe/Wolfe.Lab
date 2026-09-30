resource "forgejo_repository" "wolfe_lab" {
  owner      = var.forgejo_owner
  name       = "Wolfe.Lab"
  clone_addr = "https://github.com/tom-wolfe/Wolfe.Lab.git"
  mirror     = false
  private    = false

  lifecycle {
    ignore_changes  = [internal_tracker]
    prevent_destroy = true
  }
}

resource "forgejo_repository_push_mirror" "wolfe_lab_github" {
  provider = adyxax

  owner           = var.forgejo_owner
  repository      = forgejo_repository.wolfe_lab.name
  remote_address  = "https://github.com/tom-wolfe/Wolfe.Lab"
  remote_username = "tom-wolfe"
  remote_password = var.github_token_tom_wolfe
  sync_on_commit  = true
}
