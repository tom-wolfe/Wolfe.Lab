namespace Wolfe.Lab.Clients.Releases;

/// <summary>
/// The rehearsal: rsync's own dry run, so the log shows exactly what an install would change.
/// </summary>
internal sealed class DryRunInstaller(IWorkflowLog log, ICommandRunner commands) : IReleaseInstaller
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> Install(IDirectory source, IDirectory release, CancellationToken ct = default)
    {
        var result = await commands.Run(RsyncInstaller.Rsync(source, release, dryRun: true).QuietOutput(), ct);
        var changes = RsyncInstaller.Changes(result.StandardOutput);
        if (!release.Exists)
        {
            // The real install creates the release first; rehearsed into nothing, rsync cannot
            // mark what it would write as new, but every file of it is.
            changes = [.. changes.Select(change => change.StartsWith("~ ", StringComparison.Ordinal) ? $"+ {change[2..]}" : change)];
        }
        log.Skipped(changes.Count == 0
            ? $"Would install {source.AbsolutePath} into {release.AbsolutePath}, which already matches."
            : $"Would install {source.AbsolutePath} into {release.AbsolutePath}, changing:");
        foreach (var change in changes)
        {
            log.Detail($"  {change}");
        }

        return changes;
    }
}
