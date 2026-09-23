namespace Wolfe.Lab.Build.Workflows.Backup;

/// <summary>
/// Says what would move. Nothing reads back what was moved, so the rehearsal replaces the client.
/// </summary>
internal sealed class DryRunStateDirectories(IWorkflowLog log) : IStateDirectories
{
    /// <inheritdoc />
    public bool Move(IDirectory from, IDirectory to)
    {
        log.Skipped($"Would move {from.AbsolutePath} to {to.AbsolutePath}.");
        return from.Exists;
    }
}
