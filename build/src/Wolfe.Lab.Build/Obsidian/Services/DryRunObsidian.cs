namespace Wolfe.Lab.Build.Obsidian.Services;

/// <summary>
/// Reports the sync instead of running it.
/// </summary>
internal sealed class DryRunObsidian(IWorkflowLog log) : IObsidian
{
    /// <inheritdoc />
    public Task Sync(IDirectory vault, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would sync {vault.AbsolutePath}.");
        return Task.CompletedTask;
    }
}
