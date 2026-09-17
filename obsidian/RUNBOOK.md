# Obsidian runbook

Procedures: things done in order, at a keyboard. The design and the
reasons are in `README.md`.

## Bootstrap

Order matters — the token must exist **before** the merge, and the
vaults must be re-pointed **straight after** it: between the merge and
step 3 the obsidian workflow fails every ten minutes, which is
expected and stops on its own.

1. **Mint the token**: Forgejo → Settings → Applications → Generate
   token, name `obsidian`, scope `write:repository` only.
2. **Vault it**: item `forgejo-obsidian-token` in the Wolfe.Lab vault,
   token in `credential`.
3. **Merge.** The tofu forgejo workflow creates the two empty
   repositories. Tripwire: **2 to add**, 0 changed, 0 destroyed. The
   chezmoi workflow rewrites the runner's config with `dotnet` on its
   PATH, which the runner reads only at start: restart it
   (`forgejo/RUNBOOK.md` "Restarting a runner").
4. **Re-point each vault** on the mini. `ob` is already signed in;
   `sync-setup` prompts for the vault's end-to-end password. The vault
   IDs are what `ob sync-list-local` prints today.

   `sync-config` runs before the first pass on purpose: mirror-remote
   from the start, and every config category on, so the settings stub
   `sync-setup` writes is replaced by the vault's own before anything
   is committed.

   ```sh
   PATH="$HOME/.local/bin:$PATH"
   old="$HOME/Library/CloudStorage/GoogleDrive-trwolfe13@gmail.com/My Drive/Obsidian"
   configs=app,appearance,appearance-data,hotkey,core-plugin,core-plugin-data,community-plugin,community-plugin-data

   # Main
   nvm-run ob sync-unlink --path "$old/Main"
   mkdir -p ~/Obsidian/main
   nvm-run ob sync-setup --vault d83d080aa129a220d67b3646a264b42b \
     --path ~/Obsidian/main --device-name MacMini.local
   nvm-run ob sync-config --path ~/Obsidian/main --mode mirror-remote \
     --file-types image,audio,pdf,video --configs "$configs"
   nvm-run ob sync --path ~/Obsidian/main      # the full download; minutes
   git -C ~/Obsidian/main init -q -b main

   # Dungeons & Dragons
   nvm-run ob sync-unlink --path "$old/Dungeons & Dragons"
   mkdir -p ~/Obsidian/dnd
   nvm-run ob sync-setup --vault 48d36650fba5264cd39acb07a8bb2d51 \
     --path ~/Obsidian/dnd --device-name MacMini.local
   nvm-run ob sync-config --path ~/Obsidian/dnd --mode mirror-remote \
     --file-types image,audio,pdf,video --configs "$configs"
   nvm-run ob sync --path ~/Obsidian/dnd
   git -C ~/Obsidian/dnd init -q -b main
   ```

5. **First push.** Run each vault's workflow from the Actions tab
   (`obsidian main`, `obsidian dnd`) — the daily one will not come round
   by itself. The first run packs and pushes the whole vault, so it
   takes minutes rather than seconds; every run after it pushes only
   what changed.
6. **Confirm**: both repositories show a `Sync from Obsidian` commit
   with `.obsidian/` in it, and `nvm-run ob sync-status --path
   ~/Obsidian/main` reports the new location, `mirror-remote`, and
   the config categories. A pass can be rehearsed by hand from a
   checkout on the mini: `cd obsidian && dotnet run --project
   ../build/src/Wolfe.Lab.Build -- sync --vault main --dry-run`. An empty `.obsidian/` in the commit means the
   other devices publish no configuration — their "Vault configuration"
   toggles in Obsidian's Sync settings decide that.
7. **Revoke the runner's Full Disk Access** (System Settings → Privacy
   & Security → Full Disk Access → `forgejo-runner` off). Nothing the
   runner runs reads `~/Library/CloudStorage` any more. The stale
   copies under Google Drive go when the Drive app does.

## Adding a vault

1. `ob sync-list-remote` for its ID; add `"<name>" = "Obsidian vault: …"`
   to `locals.vaults` in `forgejo/tofu/vaults.tf`, and the vault's
   `path` and `repository` under `vaults` in `ritten.json`.
2. Copy `.forgejo/workflows/obsidian-dnd.yaml` to `obsidian-<name>.yaml`,
   change the vault name, the concurrency group and the alert title,
   and give it the schedule the vault deserves.
3. Merge, then steps 4–6 above for the new vault.

## Restore

**The mini's checkout is gone** (fresh mini, or a deleted `~/Obsidian`).
Clone first so the history continues instead of starting over, then
attach Obsidian Sync to the clone — it reconciles identical files
rather than duplicating them:

```sh
git clone http://macmini.local:3000/tom-wolfe/obsidian-main.git ~/Obsidian/main
nvm-run ob sync-setup --vault d83d080aa129a220d67b3646a264b42b \
  --path ~/Obsidian/main --device-name MacMini.local
nvm-run ob sync-config --path ~/Obsidian/main --mode mirror-remote \
  --file-types image,audio,pdf,video --configs "$configs"   # as in Bootstrap
```

**Forgejo is gone.** The repositories return with Forgejo's own restore
(`forgejo/RUNBOOK.md`); the checkout keeps pushing once it is back, and
a clean Forgejo with empty repositories takes the checkout's full
history on the next push.

**A note needs rolling back.** Not in the mini's checkout — it is a
mirror, and Obsidian Sync reverts anything written there. Take the old
version out of git on any machine and put it into a vault that does
sync up, the MacBook's copy for instance:

```sh
git -C ~/Obsidian/main log --oneline -- '10-19 Life admin/Some note.md'
git -C ~/Obsidian/main show <sha>:'10-19 Life admin/Some note.md' > /tmp/note.md
# then copy /tmp/note.md over the file in the MacBook's vault
```
