

resource "b2_bucket" "restic" {
  bucket_name = "wolfe-lab-restic"
  bucket_type = "allPrivate"

  # Restic manages its own object lifetimes (forget/prune).
  lifecycle_rules {
    file_name_prefix             = ""
    days_from_hiding_to_deleting = 1
  }
}

# The key restic actually uses.
resource "b2_application_key" "restic" {
  key_name  = "wolfe-lab-restic"
  bucket_id = b2_bucket.restic.bucket_id
  capabilities = [
    "listBuckets",
    "listFiles",
    "readFiles",
    "writeFiles",
    "deleteFiles",
  ]
}

data "healthchecksio_channel" "email" {
  kind = "email"
}

data "healthchecksio_channel" "pushover" {
  kind = "po"
}

resource "healthchecksio_check" "restic_offsite" {
  name = "lab-restic-offsite"
  desc = "Dead man's switch for the lab's offsite backup."

  schedule = "35 4 * * *"
  timezone = "Europe/London"

  # Six hours: the first-seed copy can legitimately run for hours, and a
  # genuinely dead lab already alerts within ~25 minutes via the tick's
  # check — this one exists to catch the OFFSITE copy specifically going
  # quiet, where same-morning notice is plenty.
  grace = 21600

  tags = ["lab", "backup", "restic"]

  channels = [
    data.healthchecksio_channel.pushover.id,
    data.healthchecksio_channel.email.id,
  ]
}
