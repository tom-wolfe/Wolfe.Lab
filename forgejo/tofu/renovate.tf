# Renovate's user (renovate/README.md): an identity of its own, so its pull
# requests are its own and trigger the checks like anyone's, with write
# access to Wolfe.Lab alone. Its token is minted downstream, in
# renovate/tofu, logged in as this user — the provider connects when it is
# configured, so a root that logs in as a user cannot also create it.

resource "forgejo_user" "renovate" {
  login                = "renovate"
  email                = "renovate@twolfe.dev"
  password             = var.renovate_password
  full_name            = "Renovate"
  description          = "Opens a pull request for every pin in Wolfe.Lab that is behind."
  must_change_password = false
  visibility           = "public"
}

resource "forgejo_collaborator" "renovate" {
  repository_id = forgejo_repository.wolfe_lab.id
  user          = forgejo_user.renovate.login
  permission    = "write"
}
