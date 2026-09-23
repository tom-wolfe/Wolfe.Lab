namespace Wolfe.Lab.Build.Clients.Releases;

/// <summary>
/// The rehearsal: rsync's own dry run, so the log shows exactly what an install would change.
/// </summary>
internal sealed class DryRunInstaller(IWorkflowLog log, ICommandRunner commands) : IReleaseInstaller
{
    /// <inheritdoc />
    public async Task Install(IDirectory source, IDirectory release, CancellationToken ct = default)
    {
        log.Skipped($"Would install {source.AbsolutePath} into {release.AbsolutePath}; the changes it would make:");
        await commands.Run(RsyncInstaller.Rsync(source, release, dryRun: true), ct);
    }
}
