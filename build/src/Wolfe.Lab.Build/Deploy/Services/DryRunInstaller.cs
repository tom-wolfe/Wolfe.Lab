namespace Wolfe.Lab.Build.Deploy.Services;

/// <summary>
/// rsync has its own rehearsal, which lists what would change, so a dry run shows the diff
/// the real install would make rather than saying nothing.
/// </summary>
internal sealed class DryRunInstaller(IWorkflowLog log, ICommandRunner commands) : ISliceInstaller
{
    /// <inheritdoc />
    public async Task Install(IDirectory source, IDirectory release, CancellationToken ct = default)
    {
        log.Skipped($"Would install {source.AbsolutePath} into {release.AbsolutePath}; the changes it would make:");
        await commands.Run(RsyncInstaller.Rsync(source, release, dryRun: true), ct);
    }
}
