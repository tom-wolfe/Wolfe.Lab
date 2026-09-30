# The Obsidian vaults' git repositories
locals {
  vaults = {
    "Main" = "Obsidian vault: Main"
    "Dnd"  = "Obsidian vault: Dungeons & Dragons"
  }
}

resource "forgejo_repository" "vault" {
  for_each = local.vaults

  owner          = "Obsidian"
  name           = "Wolfe.${each.key}"
  description    = each.value
  private        = true
  auto_init      = false
  default_branch = "main"

  has_actions       = false
  has_issues        = false
  has_packages      = false
  has_projects      = false
  has_pull_requests = false
  has_releases      = false
  has_wiki          = false

  lifecycle {
    ignore_changes  = [internal_tracker]
    prevent_destroy = true
  }
}
