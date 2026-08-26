# Every <slice>/<job>/flow.yaml in the repo becomes a managed flow — this
# is kestra's tofu root managing kestra's API resources, the same way
# forgejo/tofu manages repositories. The file's own id and namespace are
# the source of truth; tofu mirrors them into the resource so a rename in
# the YAML is a rename in the plan — no second place to edit.
locals {
  repo = "${path.module}/../.."

  # One directory per job, under each slice's flows/ marker:
  # <slice>/flows/<job>/flow.yaml (+ script.sh beside it). The literal
  # `flows` segment is the semantic boundary — nothing outside a flows/
  # directory can ever be applied, no matter what files it contains.
  flow_files = fileset(local.repo, "*/flows/*/flow.yaml")
}

resource "kestra_flow" "all" {
  for_each = local.flow_files

  flow_id   = yamldecode(file("${local.repo}/${each.value}"))["id"]
  namespace = yamldecode(file("${local.repo}/${each.value}"))["namespace"]
  content   = file("${local.repo}/${each.value}")
}

# One-time renames from the pre-slice layout (this root was tofu/kestra,
# flows keyed as flows/<name>.yaml). Delete after the first apply.
moved {
  from = kestra_flow.all["flows/chezmoi-update.yaml"]
  to   = kestra_flow.all["chezmoi/flows/update/flow.yaml"]
}

moved {
  from = kestra_flow.all["flows/obsidian-sync-main.yaml"]
  to   = kestra_flow.all["obsidian-sync/flows/main/flow.yaml"]
}

moved {
  from = kestra_flow.all["flows/obsidian-sync-dnd.yaml"]
  to   = kestra_flow.all["obsidian-sync/flows/dnd/flow.yaml"]
}
