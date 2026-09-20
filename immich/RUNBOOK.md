# Immich runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap

1. **Room for it.** Docker Desktop on the mini → Settings → Resources →
   Memory: 12 GB (the default is half the machine, and Immich's own
   floor with machine learning is 6 GB beside what already runs).
   Apply & restart. Screen Sharing on `:5900` reaches it.
2. **The database password**: item `immich-postgres` in the Wolfe.Lab
   vault, a generated password of letters and digits only in
   `credential`, before the merge.
3. **Merge.** The immich workflow installs the slice and converges the
   stack; the caddy workflow picks up the route; gatus starts probing.
   Postgres initialises on first start, which takes a minute, and the
   machine-learning container downloads its models on its first job.
4. **The admin account.** The first visitor to
   `https://immich.twolfe.dev` creates it: do that straight after
   the deploy goes green. Item `immich-admin` in the vault, a Login item.
5. **The API key** for the import: Account settings → API keys → New,
   named `lab import`. Item `immich-api-key`, key in `credential`.
6. **Import.** Actions → "immich import" → Run workflow. Hours, and it
   can be re-run if the job is cut short. Watch it in the Actions log;
   the counts at the end are what to compare against Google's.
7. **Let machine learning catch up** — Administration → Jobs shows the
   queues draining over a day or two.
8. **The backup needs nothing turned on.** Its job in
   `.forgejo/workflows/immich-backup.yaml` runs nightly from the merge, and a
   warm snapshot of a half-imported library is just a smaller snapshot;
   retention prunes it. The job has its own three-hour timeout for the
   first pass over the finished library, which is the long one.
9. **The offsite seed** takes several nights: the nightly copy ships as
   much as fits its window and resumes. It is done when
   `restic snapshots` on the offsite side lists the immich snapshot and
   the Sunday verify has passed.
10. **Then, and only then, delete the Takeout zips** from
    `/Volumes/Data2/photos/google`.
11. **The phone**: install the Immich app, server
    `https://immich.twolfe.dev`, sign in, turn on background backup.
    Turn off Google Photos backup.

## Upgrading

Pinned in `compose.yaml`: the server and machine-learning tags together,
the Postgres and Valkey digests separately. Immich's release notes are
where breaking changes live, and it has them often; read them before a
bump, and never skip a major. Then a normal PR — the push deploys it.
Immich migrates its database forward on start; the snapshot the backup
job took at 03:00 is the way back.

## Restore

**The database**: Administration → Maintenance → Restore database backup,
from the dumps in `backups/`, which restic holds along with everything
else. Immich takes a restore point first.

**The library**, from restic, into `/Volumes/Data2/immich`:

```sh
cd immich
dotnet run --project ../build/src/Wolfe.Lab.Build -- restore
```

The library is set aside as `/Volumes/Data2/immich.bak-<timestamp>`
and the snapshot restored in its place (`restic/RUNBOOK.md` "Restore").

Then Administration → Jobs → run the thumbnail and transcode jobs to
regenerate what was excluded.

**Everything, on a fresh mini**: deploy, restore the library as above,
start the stack, restore the database from the UI, regenerate.
