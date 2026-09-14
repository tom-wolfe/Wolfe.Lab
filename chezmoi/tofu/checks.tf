# The dead man's switch. Every alert path in this lab runs on the mini's
# Actions runner and dies with it (or with Forgejo, or the mini), so the
# one signal that can report "the lab is off" has to come from outside:
# .forgejo/workflows/heartbeat.yaml pings this check every 15 minutes, and
# healthchecks.io shouts when the pings stop.
#

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
    Dead man's switch for the lab's scheduler: .forgejo/workflows/
    heartbeat.yaml pings every 15 minutes from the mini's runner. Silence
    means Forgejo, the runner or the mini stopped, or the network is gone.
    The slug is historical; renaming would recreate the check. Managed by chezmoi/tofu — edits
    here are reverted.
  EOT

  # Cron mode rather than a simple period, so the expectation mirrors the
  # heartbeat workflow's own schedule exactly and a missed slot is caught
  # at that slot, not a fixed interval later.
  schedule = "*/15 * * * *"
  timezone = "Europe/London"

  # 10 minutes: one HTTP call needs seconds, and this is comfortably
  # shorter than two missed slots, so a genuine stall alerts inside half
  # an hour.
  grace = 600

  tags = ["lab", "actions"]

  channels = [
    data.healthchecksio_channel.pushover.id,
    data.healthchecksio_channel.email.id,
  ]
}
