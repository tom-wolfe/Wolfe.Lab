# Renovate's tokens, as the Actions secrets its workflow reads
# (renovate/README.md). A containerised job never reaches the vault, so these
# are the lab's only Actions secrets. Rotating Renovate's own token is
# `tofu apply -replace=forgejo_personal_access_token.renovate`.

data "forgejo_repository" "wolfe_lab" {
  owner = "tom-wolfe"
  name  = "Wolfe.Lab"
}

# The scopes Renovate's Forgejo platform documents: branches and pull
# requests, the dependency dashboard issue, its own user and the owner's
# labels; and packages, for the lab's own NuGet feed.
resource "forgejo_personal_access_token" "renovate" {
  provider = forgejo.renovate

  user = "renovate"
  name = "renovate"
  scopes = [
    "write:repository",
    "write:issue",
    "read:user",
    "read:organization",
    "read:package",
  ]
}

resource "forgejo_repository_action_secret" "renovate_token" {
  repository_id = data.forgejo_repository.wolfe_lab.id
  name          = "RENOVATE_TOKEN"
  data          = forgejo_personal_access_token.renovate.token
}

resource "forgejo_repository_action_secret" "renovate_github_com_token" {
  repository_id = data.forgejo_repository.wolfe_lab.id
  name          = "RENOVATE_GITHUB_COM_TOKEN"
  data          = var.renovate_github_token
}
