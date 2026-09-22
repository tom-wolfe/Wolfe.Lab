using Wolfe.Lab.Build.Clients.Obsidian;
using Wolfe.Lab.Build.Workflows.Obsidian.Models;

namespace Wolfe.Lab.Build.Workflows.Obsidian.Steps;

/// <summary>
/// One pass of Obsidian Sync: the checkout is a mirror, so this is where the vault's changes arrive.
/// </summary>
[Step("sync vault", StepKind.Work)]
internal sealed class SyncVault(IObsidian obsidian, IWorkflowLog log)
{
    public async Task<StepResult> Run(Vault vault, CancellationToken ct = default)
    {
        log.Status($"Syncing {vault.Name} into {vault.Directory.AbsolutePath}.");
        await obsidian.Sync(vault.Directory, ct);
        return StepResult.Successful;
    }
}
