using Wolfe.Lab.Domain.Catalog.Components.Obsidian;
using Wolfe.Lab.Infrastructure.Obsidian;

namespace Wolfe.Lab.Application.Workflows.Obsidian.Steps;

/// <summary>
/// One pass of Obsidian Sync: the checkout is a mirror, so this is where the vault's changes arrive.
/// </summary>
[Step("sync vault", StepKind.Work)]
internal sealed class SyncVault(IObsidian obsidian, IWorkflowLog log)
{
    public async Task<StepResult> Run(ObsidianComponent vault, CancellationToken ct = default)
    {
        log.Status($"Syncing {vault.Name} into {vault.Path.Directory.AbsolutePath}.");
        await obsidian.Sync(vault.Path.Directory, ct);
        return StepResult.Successful;
    }
}
