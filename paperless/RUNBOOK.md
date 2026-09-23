# Paperless-ngx runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap

1. **The vault items**, before the merge: `paperless-secret-key`, a
   generated password of sixty-four characters or more in `credential`;
   `paperless-admin`, a Login item with the username and a generated
   password.
2. **Merge.** The `paperless compose` workflow installs the stack and converges
   it; the `caddy routes` workflow picks up the route; gatus starts probing.
   The first start runs the database migrations and takes a minute.
3. **Sign in** at `https://paperless.twolfe.dev` as the admin item.
4. **The first document**: drag a PDF into the UI and watch it consume.
   From then on the nightly backup carries an archive, and
   `media/documents/originals` can join the `verify` list in
   `backup/ritten.json` once it has something in it.
5. **The phone**: install Paperless Mobile or Swift Paperless, server
   `https://paperless.twolfe.dev`, sign in.

## Upgrading

Pinned in `compose.yaml`: the Paperless tag and the Valkey digest
separately. Paperless's release notes are where breaking changes live —
a major bump changes the database layout and the export format — so
read them before a bump, and never skip a major. Snapshot first:

```sh
cd paperless/backup
dotnet run --project ../../build/src/Wolfe.Lab.Build -- backup
```

Then a normal PR; the push deploys it. Paperless migrates its database
forward on start, and the snapshot is the way back.

Check what's current:

```sh
curl -s "https://api.github.com/repos/paperless-ngx/paperless-ngx/releases/latest" | grep '"tag_name"'
```

## Backup

Runs itself: `.forgejo/workflows/paperless-backup.yaml` snapshots this
slice nightly at 02:55 and drills the restore straight after. Manual
snapshot — run the backup workflow from the Actions tab, or:

```sh
cd paperless/backup
dotnet run --project ../../build/src/Wolfe.Lab.Build -- backup
```

The stack is stopped for the duration — SQLite copied mid-write can be
inconsistent, and a document half-consumed would be in neither place —
so expect a minute of downtime. A file dropped in the inbox meanwhile
waits; the consumer picks it up on restart.

## Restore

```sh
cd paperless/backup
dotnet run --project ../../build/src/Wolfe.Lab.Build -- restore
```

`restic/RUNBOOK.md` "Restore" for what it does and its options. The
snapshot carries no search index and no thumbnails; regenerate both
once the stack is up:

```sh
cd ~/.local/share/Wolfe.Lab/paperless
docker compose exec paperless document_index reindex
docker compose exec paperless document_thumbnails
```

The job refuses to restore onto an image other than the one the
snapshot was taken under; pin `compose.yaml` first.

**Everything, on a fresh mini**: deploy, restore as above, regenerate.
The admin login is in the database, so the vault's `paperless-admin`
is a copy from that point and the first-start pair is ignored.
