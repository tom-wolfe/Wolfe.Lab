# The dead man's switch for the CD tick.
#
# Everything else that watches this lab runs INSIDE it — system/alert-failed
# is a Kestra flow, so it dies with Kestra, with postgres, with the mini, or
# with the power. Silence from a dead lab is indistinguishable from silence
# from a healthy one. This check is the only observer outside the building:
# lab.chezmoi/update pings it on success, and healthchecks.io shouts when
# the pings STOP.
#
# Ping by SLUG, not UUID (https://hc-ping.com/<ping-key>/<slug>), which is
# why the flow composes its URL from a project-level ping key instead of
# storing a per-check URL. Three consequences, all wanted:
#   * the URL survives this check being destroyed and recreated — a UUID
#     would not, and the breakage would be silent until the next outage;
#   * ONE secret (the ping key) covers every future check, so adding one is
#     a resource here plus a task in the flow — no new vault item, no
#     chezmoi change, no container restart;
#   * the slug is visible in the flow next to the thing it monitors.
# The slug is derived from `name` by healthchecks.io, so `name` is written
# slug-shaped and the two stay identical. VERIFY after the first apply that
# the check's slug really is `lab-chezmoi-update` (Check Details -> the ping
# URL) — if it isn't, pings 404 and healthchecks reports the tick as down.
# That failure is loud, not silent, which is why deriving is acceptable.

# Notification channels are configured in the healthchecks.io UI and only
# REFERENCED here — the provider reads them, it doesn't create them, so a
# data source for a channel that hasn't been set up fails the plan.
#
# Both, deliberately. Pushover is the one that actually reaches a phone, and
# it lands beside the in-lab alerts from system/alert-failed — same device,
# same app, while THIS alert's origin stays outside the lab, which is the
# entire point of it. Email is the backstop for the case Pushover itself is
# the thing that's broken; it always exists (it's the account address) and
# costs nothing to keep.
#
# `kind` values are healthchecks.io's internal transport keys, not display
# names: Pushover is "po" (see hc/api/models.py TRANSPORTS). Each lookup
# matches by kind alone, so a second integration of the same kind would make
# these ambiguous — add the optional `name` argument if that ever happens.
data "healthchecksio_channel" "email" {
  kind = "email"
}

data "healthchecksio_channel" "pushover" {
  kind = "po"
}

resource "healthchecksio_check" "chezmoi_tick" {
  # Slug-shaped on purpose — see the note above.
  name = "lab-chezmoi-update"
  desc = <<-EOT
    Dead man's switch for the Wolfe.Lab CD tick (Kestra flow
    lab.chezmoi/update). Pinged on every SUCCESSful converge. Silence means
    the tick stopped: Kestra down, postgres wedged, the mini off, or the
    network gone. Managed by chezmoi/tofu — edits here are reverted.
  EOT

  # Cron mode rather than a simple period, so the expectation mirrors the
  # flow's own schedule exactly and a tick missing its slot is caught at
  # that slot, not a fixed interval later.
  schedule = "*/15 * * * *"
  timezone = "Europe/London"

  # 10 minutes: comfortably longer than a slow converge (chezmoi update on a
  # cold Brewfile can run minutes) and comfortably shorter than two missed
  # ticks, so a genuine stall alerts inside half an hour.
  grace = 600

  tags = ["lab", "tick", "kestra"]

  channels = [
    data.healthchecksio_channel.pushover.id,
    data.healthchecksio_channel.email.id,
  ]
}
