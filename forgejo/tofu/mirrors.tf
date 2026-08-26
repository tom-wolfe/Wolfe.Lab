locals {
  github_tokens = {
    "tom-wolfe"    = var.github_token_tom_wolfe
    "nschema-org"  = var.github_token_nschema_org
    "DisasterCare" = var.github_token_disastercare
  }

  # GitHub owner -> Forgejo owner. The Forgejo orgs mirror the GitHub org
  # structure but were created with cleaner names.
  forgejo_owners = {
    "tom-wolfe"    = "tom-wolfe"
    "hamelin-org"  = "Hamelin"
    "nschema-org"  = "NSchema"
    "ritten-org"   = "Ritten"
    "DisasterCare" = "DisasterCare"
  }

  # mode = "mirror": read-only pull mirror, synced on var.mirror_interval.
  # mode = "active": writable repo, cloned ONCE from GitHub at creation.
  #
  # !! Flipping mode REPLACES the Forgejo repository (destroy + re-clone).
  # !! Before flipping active -> mirror, push any work you care about —
  # !! the Forgejo copy is destroyed. See README.md.
  repos = {
    # --- DisasterCare ----------------------------------------------------
    "Abodio" = { owner = "DisasterCare", private = true, mode = "mirror" }

    # --- hamelin-org -----------------------------------------------------
    "Hamelin"                         = { owner = "hamelin-org", private = false, mode = "mirror" }
    "Hamelin.Runtimes.AzurePipelines" = { owner = "hamelin-org", private = false, mode = "mirror" }
    "Hamelin.Runtimes.GitHubActions"  = { owner = "hamelin-org", private = false, mode = "mirror" }

    # --- nschema-org -----------------------------------------------------
    "NSchema"           = { owner = "nschema-org", private = false, mode = "mirror" }
    "NSchema.Aws"       = { owner = "nschema-org", private = false, mode = "mirror" }
    "NSchema.Build"     = { owner = "nschema-org", private = true, mode = "mirror" }
    "NSchema.Core"      = { owner = "nschema-org", private = false, mode = "mirror" }
    "NSchema.Docs"      = { owner = "nschema-org", private = false, mode = "mirror" }
    "NSchema.Gauntlet"  = { owner = "nschema-org", private = false, mode = "mirror" }
    "NSchema.Iac"       = { owner = "nschema-org", private = true, mode = "mirror" }
    "NSchema.Northwind" = { owner = "nschema-org", private = true, mode = "mirror" }
    "NSchema.Postgres"  = { owner = "nschema-org", private = false, mode = "mirror" }
    "NSchema.SqlServer" = { owner = "nschema-org", private = false, mode = "mirror" }
    "NSchema.Sqlite"    = { owner = "nschema-org", private = false, mode = "mirror" }

    # --- ritten-org ------------------------------------------------------
    "Ritten" = { owner = "ritten-org", private = false, mode = "mirror" }

    # --- tom-wolfe (Wolfe.Lab is the Forgejo primary — see primary.tf) ---
    "Wolfe.CharacterCompanion"  = { owner = "tom-wolfe", private = true, mode = "mirror" }
    "Wolfe.Games"               = { owner = "tom-wolfe", private = true, mode = "mirror" }
    "Wolfe.Scrumdinger"         = { owner = "tom-wolfe", private = true, mode = "mirror" }
    "Wolfe.Tabletop"            = { owner = "tom-wolfe", private = true, mode = "mirror" }
    "Wolfe.Templates"           = { owner = "tom-wolfe", private = true, mode = "mirror" }
    "Wolfe.Tools"               = { owner = "tom-wolfe", private = true, mode = "mirror" }
    "brewdown"                  = { owner = "tom-wolfe", private = false, mode = "mirror" }
    "dice-typescript"           = { owner = "tom-wolfe", private = false, mode = "mirror" }
    "dnd-5e-tools"              = { owner = "tom-wolfe", private = false, mode = "mirror" }
    "expressionTS"              = { owner = "tom-wolfe", private = false, mode = "mirror" }
    "gulp-markdownit"           = { owner = "tom-wolfe", private = false, mode = "mirror" }
    "james-hoffmann-calculator" = { owner = "tom-wolfe", private = false, mode = "mirror" }
    "markov-typescript"         = { owner = "tom-wolfe", private = false, mode = "mirror" }
    "selectable-ts"             = { owner = "tom-wolfe", private = false, mode = "mirror" }
    "this-is-your-life"         = { owner = "tom-wolfe", private = false, mode = "mirror" }
    "tiyl"                      = { owner = "tom-wolfe", private = false, mode = "mirror" }
    "twolfe.dev"                = { owner = "tom-wolfe", private = true, mode = "mirror" }
  }
}

resource "forgejo_repository" "repo" {
  for_each = local.repos

  owner           = local.forgejo_owners[each.value.owner]
  name            = each.key
  clone_addr      = "https://github.com/${each.value.owner}/${each.key}.git"
  mirror          = each.value.mode == "mirror"
  mirror_interval = each.value.mode == "mirror" ? var.mirror_interval : null
  private         = each.value.private
  auth_token      = each.value.private ? local.github_tokens[each.value.owner] : null

  # svalabs provider (still in 1.6.0) re-plans these computed blocks as
  # unknown on every run, producing perpetual no-op updates whose PATCH can
  # 500 on repos with wikis ("'' is not a valid branch name"). Upstream:
  # svalabs/terraform-provider-forgejo#132, #169. Remove once fixed.
  #
  # NB: if a repo is deleted outside tofu, refresh errors instead of
  # re-creating — intentional per upstream #111. Recover with:
  #   tofu state rm 'forgejo_repository.repo["<name>"]'
  lifecycle {
    ignore_changes = [internal_tracker]
  }
}