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

  # What each file DECLARES, beside what its PATH requires of it.
  flows = {
    for f in local.flow_files : f => {
      declared_namespace = yamldecode(file("${local.repo}/${f}"))["namespace"]
      declared_id        = yamldecode(file("${local.repo}/${f}"))["id"]
      # <slice>/flows/<job>/flow.yaml -> lab.<slice> / <job>
      required_namespace = "lab.${split("/", f)[0]}"
      required_id        = split("/", f)[2]
    }
  }

  # The ONLY flows allowed outside the `lab.` prefix.
  #
  # This list is a safety boundary, not bookkeeping. system/alert-failed
  # triggers on NAMESPACE STARTS_WITH "lab.", so a flow outside that prefix
  # is INVISIBLE to alerting — it can fail every night forever and nothing
  # will say so. Adding an entry here is a decision to make a flow
  # unmonitored, and it should be as hard to do by accident as possible.
  #
  # alert-failed itself qualifies because it must not match its own trigger,
  # or a failed alert would alert, fail, and loop.
  unmonitored = [
    "kestra/flows/alert-failed/flow.yaml",
  ]
}

resource "kestra_flow" "all" {
  for_each = local.flows

  flow_id   = each.value.declared_id
  namespace = each.value.declared_namespace
  content   = file("${local.repo}/${each.key}")

  lifecycle {
    # The naming convention documented in ../README.md, enforced. It used to
    # say "nothing enforces it", which was fine while it was tidiness — but
    # the convention became load-bearing the moment alerting keyed off the
    # `lab.` prefix. A typo'd namespace doesn't fail, it just quietly drops
    # a flow out of alert coverage, and nothing anywhere would report it.
    precondition {
      condition = contains(local.unmonitored, each.key) || (
        each.value.declared_namespace == each.value.required_namespace &&
        each.value.declared_id == each.value.required_id
      )
      error_message = format(
        "%s declares '%s/%s' but its path requires '%s/%s'. Flow identity is derived from location: <slice>/flows/<job>/ -> lab.<slice>/<job>. Fix the YAML, or move the file. Only if the flow is genuinely meant to be UNMONITORED by system/alert-failed should it go outside the 'lab.' prefix, and then it must be added to local.unmonitored in flows.tf.",
        each.key,
        each.value.declared_namespace, each.value.declared_id,
        each.value.required_namespace, each.value.required_id,
      )
    }
  }
}
