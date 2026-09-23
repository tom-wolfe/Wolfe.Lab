# Branch protection for main: a pull request cannot merge until its checks
# have passed.
#
# The trailing wildcard on each is only for the event Forgejo suffixes onto
# the context — "build / check (pull_request)" — which is not worth pinning.
#
# None of these is path-filtered, and none should be: a check that does not
# run posts no status, and a named context with no status is a merge that
# never unblocks. Skipping the WORK when a component has not changed belongs
# inside the job, where the check can read what the branch changed and
# finish early while still posting the status this list requires.
resource "forgejo_branch_protection" "main" {
  repository_id = forgejo_repository.wolfe_lab.id
  branch_name   = "main"

  enable_status_check = true
  status_check_contexts = [
    "build / check*",
    "ci image / check*",
    "chezmoi / check*",
    "mail bridge / check*",
    "mail watcher / check*",
    "caddy tofu / check*",
    "heartbeat tofu / check*",
    "dns tofu / check*",
    "forgejo tofu / check*",
    "gatus tofu / check*",
    "restic tofu / check*",
    "tailscale tofu / check*",
    "beszel compose / check*",
    "caddy compose / check*",
    "forgejo compose / check*",
    "garage compose / check*",
    "gatus compose / check*",
    "immich compose / check*",
    "jellyfin compose / check*",
    "paperless compose / check*",
    "qbittorrent compose / check*",
    "radarr compose / check*",
    "sonarr compose / check*",
  ]

  # A pull request must be current with main before it merges, so the checks
  # that passed are the checks for what main will actually contain.
  block_on_outdated_branch = true

  # Nobody else is here to approve, and Forgejo will not let an author
  # approve their own pull request — requiring one would be requiring the
  # impossible.
  required_approvals = 0

  # Direct pushes stay allowed. Closing that is the stronger form of this
  # and would mean every change to the lab goes through a pull request,
  # which is a workflow decision rather than a protection one.
  enable_push = true
}
