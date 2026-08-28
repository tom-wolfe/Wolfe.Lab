# obsidian

One-shot syncs of the Obsidian vaults from Google Drive: `main/` and
`dnd/`, each a scheduled pass every 10 minutes, offset by five so the two
never contend for Drive at the same moment. The host-side scripts run
`nvm-run ob sync` against the vault's CloudStorage path.

These replaced the `md.obsidian.headless-sync-*` launch agents'
`--continuous` watchers — trading ≤10 min of latency for logs, retries,
and failure visibility (the cutover pattern is documented in
`kestra/README.md`).

Adding a vault = one more `flows/<name>/` directory (`flow.yaml` + `script.sh`);
the id, schedule and vault path all live inside the pair.
