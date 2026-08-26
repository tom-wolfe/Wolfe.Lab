# Shape 3: Wolfe.Lab lives on Forgejo as the primary, push-mirrored to
# GitHub as backup/showcase. clone_addr imports the full history from
# GitHub once at creation; after that, Forgejo is the source of truth and
# every push flows back to GitHub automatically (sync_on_commit).
#
# The mini's chezmoi and any GitHub-pulling machinery keep working
# unchanged: the GitHub copy stays current via the push mirror.

resource "forgejo_repository" "wolfe_lab" {
  owner      = var.forgejo_owner
  name       = "Wolfe.Lab"
  clone_addr = "https://github.com/tom-wolfe/Wolfe.Lab.git"
  mirror     = false
  private    = false

  # Same perpetual-diff workaround as mirrors.tf — see comment there.
  lifecycle {
    ignore_changes = [internal_tracker]
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
