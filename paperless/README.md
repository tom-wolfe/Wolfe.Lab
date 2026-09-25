# Paperless-ngx — the paperwork

The document archive: Paperless-ngx, pinned, two containers from its
reference compose bent to the lab's rules. Scans and PDFs go in, come
out OCR'd, tagged and searchable, and the originals are kept untouched
beside the archived copies. It runs on the mini because it is an
always-on ingest service whose OCR is CPU-bound: it wants to accept a
document whether or not any desktop is awake.

| | |
|---|---|
| Web UI | https://paperless.twolfe.dev (fallback http://macmini.local:8000) |
| Image | `ghcr.io/paperless-ngx/paperless-ngx` pinned in `compose.yaml`; the broker is Valkey at immich's digest |
| State | `~/Docker/paperless/data` — `db.sqlite3`, the search index, the classifier |
| Documents | `/Volumes/Data2/paperless/media/documents` — `originals/`, `archive/`, `thumbnails/` |
| Inbox | `/Volumes/Data2/paperless/consume` — anything dropped here is consumed and removed |
| Backups | `paperless-backup.yaml`, nightly at 02:55, into the restic repo (`restic/README.md`); the restore drilled straight after |

## SQLite, not Postgres

Paperless supports three databases and defaults to SQLite. This slice
keeps the default: one user, a database that stays small, and a backup
that is the same stop-snapshot-verify pipeline every SQLite slice runs
(`backup/ritten.json`). Postgres would mean a third container and a dump job
of its own, as immich has, for a concurrency the archive will not see.
The document exporter is the migration path if that ever changes.

The broker is the one piece that is not optional: Paperless queues
consumption, OCR and the scheduled tasks through it. It holds nothing
durable — a lost queue is a document consumed again.

## Where the documents live

Split the way immich is: the database, index and classifier on the
internal disk beside the other slices' state, where SQLite wants an
SSD and the whole of it stays small; the documents, the inbox and the
export directory on Data2 with the rest of the media, because the
internal disk has no room for an archive that only grows. Everything
under `media/` is irreplaceable in the way photos are, so it is in
restic and therefore in the offsite copy.

Data2 has to be mounted before the stack starts. Both `compose/ritten.json`
and `backup/ritten.json` declare the volume, so a deploy or a backup with the drive missing refuses
rather than writing to an empty directory on the internal disk.

The snapshot is cold: `lab backup` stops the stack, so nothing is
mid-write in the database or half-consumed in the inbox. `data/index`
and `media/documents/thumbnails` are excluded — Paperless regenerates
both (`RUNBOOK.md` "Restore") — as is `data/log`. The drill asserts
`db.sqlite3` and nothing else, because the originals directory is empty
until the first document and a directory the drill asserts must come
back with contents.

## Getting documents in

Three doors, all to the same consumer:

- **The web UI**, drag and drop — the ordinary case.
- **The inbox**, `/Volumes/Data2/paperless/consume` on the mini: a file
  landed there by any means (Finder over the tailnet, a scanner that
  can write to a folder, `scp`) is consumed within thirty seconds. The
  consumer polls rather than watches, because file events do not cross
  VirtioFS reliably.
- **The mobile app** — Paperless Mobile or Swift Paperless — against
  `https://paperless.twolfe.dev`, which is what turns a phone camera
  into the scanner.

Mail is a fourth door Paperless has and this slice does not open:
`mail/` runs Proton Bridge, so a mail rule polling an IMAP folder is a
configuration in the UI rather than anything here. Not configured.

## AI

Paperless's optional AI features, against the lab's own model endpoint
(`ollama/`): nothing leaves the house.

- **Suggestions** — title, tags, correspondent, document type, storage
  path and dates, from the "Suggest" control on a document, and
  requested automatically when an inbox document is opened. Beside the
  classifier's suggestions, not instead of them.
- **Document chat** — the toolbar's chat, over the open document or the
  whole archive.
- **The index** — every document's text as embeddings, rebuilt nightly
  at 02:10 (`PAPERLESS_LLM_INDEX_TASK_CRON`'s default), which grounds
  both of the above in similar documents already filed. It lives in
  `data/`, so the backup holds it.

It asks for roles, not models (`ollama/README.md`, "Roles"):
`lab/interactive`, because everything here is a person waiting, and
`lab/embedding`. By day that is the Studio's model, overnight the
mini's; with neither, suggestions and chat fail and nothing else does.

**All of it is configured in `compose.yaml`.** The UI's Application
Configuration has the same settings, and a value saved there silently
wins over the environment — leave those fields empty.

**The "Apply AI Suggestions" workflow action is not in use.** It asks
the model for every matching document unattended, which is
`lab/background`'s job, and Paperless takes one model for everything:
turning it on means switching `PAPERLESS_AI_LLM_MODEL` to
`lab/background`, and narrowing its trigger, since each document holds
the task queue while the model answers.

## Secrets

Two vault items, resolved at deploy into the one `compose up` that
creates the container (`README.md` at the root, "How deployment works"):

- **`paperless-secret-key`** — Django's signing key. Any long random
  string; Paperless refuses to start without one, and changing it logs
  every session out.
- **`paperless-admin`** — a Login item. The username and password
  create the superuser on the stack's first start and are ignored
  from then on, so a later password change in the UI does not drift
  from the vault by itself: mirror it.

## Deliberately not configured

**Tika and Gotenberg** — the two extra containers that let Paperless
consume Office documents and email files. PDFs and images cover
paperwork; add them when the first `.docx` matters.

**A filename format** — the on-disk names under `originals/` are
Paperless's document ids. A human-readable tree is a storage path
set in the UI, which is data the backup already holds, not a config
here.

**The trash directory** — deleted documents are really deleted. The
UI's trash holds them for thirty days first.

## Notes

- The image ships its own healthcheck, so `compose.yaml` deliberately
  doesn't define one. Gatus probes the login page through the front
  door; that is what alerts.
- The container starts as root and drops to `USERMAP_UID`, chowning its
  directories on the way — so the four bind mounts end up owned by the
  host user, as every slice's do.
- The restore sets `media/` aside as `/Volumes/Data2/paperless/media.bak-<timestamp>`
  beside it; delete that once the restored archive has been looked at.
- Nothing here is exposed to the internet; the name resolves to the
  mini's Tailscale address like every other.

## Runbook

Bootstrap, upgrade, backup and restore procedures are in `RUNBOOK.md`.
