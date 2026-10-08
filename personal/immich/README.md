# immich

The photo library: Immich, pinned, four containers from its reference
compose bent to the lab's rules. Originals live at `/Volumes/Data2/immich`
with the rest of the media; the database on the internal disk beside the
other services' state, where Postgres wants an SSD; the machine-learning
models beside it. The phone app talks to `immich.twolfe.dev`, the
tailnet twin of the front-door name, and its background upload is what
replaces Google Photos as the place a photo lands after it is taken.

Two deployments, each a job through the CLI in `platform/lab/`: `compose/`
(`lab deploy` installs the stack and converges it) and `backup/` (what
restic keeps of the library). The library came in from a Google Takeout
through immich-go, once; that import is in the repository's history, not
in the lab.

## What is backed up, and what is not

The lab's media splits on one question: can it be re-acquired? Films and
television can, so they are not backed up and the loss of a drive is a
re-download. Photos and videos taken on a phone cannot, so this library
is in restic and therefore in the offsite copy, at about a pound a month
for the size of it.

The snapshot is warm — no stop window, unlike the SQLite services. The
originals under `library/`, `upload/` and `profile/` are write-once, and
the database comes back from Immich's own nightly dump in `backups/`,
taken at 02:00 before the 03:00 pass and the documented restore path;
the Postgres directory is not what a restore uses. `thumbs/` and
`encoded-video/` are excluded: Immich regenerates them from the
originals, as Jellyfin regenerates its artwork.

## Machine learning

Runs on the mini's CPU. Search and faces come from the same models
wherever they run, so the Studio would only earn the job with a larger
model; if that day comes, Immich takes a remote machine-learning URL
and nothing here moves. The models unload after five minutes idle, so
the steady state is small; the first pass over the library is the peak,
and the Docker VM is sized for it (`RUNBOOK.md`).
