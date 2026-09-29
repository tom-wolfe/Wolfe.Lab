# files

The personal files: what Google Drive held, and the archives that had no
other home — old game saves, exported chats. Data, not a service: nothing
runs, nothing deploys, and the only job is the backup.

| | |
|---|---|
| Where | `/Volumes/Data2/files` |
| Backups | restic, nightly, warm (`ritten.json`); offsite with everything else (`restic/README.md`) |

## Why it is backed up

The lab's media rule splits on whether a thing can be re-acquired.
Nothing here can, so the whole directory is in restic and rides the
offsite copy. The snapshot is warm: there is no stack to stop, and
nothing writes here but a person at a keyboard.

## Adding to it

Put the files under `/Volumes/Data2/files`; the nightly snapshot picks
them up. Anything irreplaceable that lands elsewhere on the drive is
**not** backed up — this directory is the one place on it that is.

## Restore

`restic/RUNBOOK.md` "Restore", selecting `--tag service:files`; the
target is the directory itself.
