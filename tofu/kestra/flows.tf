# Every YAML file in flows/ becomes a managed flow. The file's own id and
# namespace are the source of truth; tofu mirrors them into the resource so
# a rename in the YAML is a rename in the plan — no second place to edit.
resource "kestra_flow" "all" {
  for_each = fileset(path.module, "flows/*.yaml")

  flow_id   = yamldecode(file("${path.module}/${each.value}"))["id"]
  namespace = yamldecode(file("${path.module}/${each.value}"))["namespace"]
  content   = file("${path.module}/${each.value}")
}
