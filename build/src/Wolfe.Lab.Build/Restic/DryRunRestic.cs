using Wolfe.Lab.Build.Backup.Models;

namespace Wolfe.Lab.Build.Restic;

/// <summary>
/// restic has its own rehearsal, which lists what a backup would add, so a dry run shows the
/// snapshot the real one would take rather than saying nothing.
/// </summary>
internal sealed class DryRunRestic(IWorkflowLog log, ICommandRunner commands) : IRestic
{
    /// <summary>
    /// What the rehearsal hands back in a snapshot's place.
    /// </summary>
    internal static Snapshot Rehearsed { get; } = new("rehearsed");

    /// <inheritdoc />
    public async Task<Snapshot> Backup(ResticRepository repository, IReadOnlyList<IDirectory> paths, IReadOnlyList<string> excludes, IReadOnlyList<string> tags, CancellationToken ct = default)
    {
        log.Skipped($"Would snapshot {string.Join(", ", paths.Select(p => p.AbsolutePath))} into {repository.Location}; what it would add:");
        await commands.Run(ResticClient.BackupCommand(repository, paths, excludes, tags, dryRun: true), ct);
        return Rehearsed;
    }

    /// <inheritdoc />
    public Task Forget(ResticRepository repository, Snapshot snapshot, CancellationToken ct = default)
    {
        log.Skipped($"Would forget snapshot {snapshot.Id}.");
        return Task.CompletedTask;
    }
}
