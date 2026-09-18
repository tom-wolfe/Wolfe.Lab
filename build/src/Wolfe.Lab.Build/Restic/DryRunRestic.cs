using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Restic.Models;

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

    /// <inheritdoc />
    public Task Copy(OffsiteRepository offsite, CancellationToken ct = default)
    {
        log.Skipped($"Would copy new snapshots to {offsite.Repository.Location}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task Prune(ResticRepository repository, RetentionPolicy policy, CancellationToken ct = default)
    {
        log.Skipped($"Would apply retention to {repository.Location}; what it would forget:");
        await commands.Run(ResticClient.PruneCommand(repository, policy, dryRun: true), ct);
    }

    /// <inheritdoc />
    public async Task Check(ResticRepository repository, string? readDataSubset, CancellationToken ct = default)
    {
        // The structural check is a read and goes through; the data sample is the part that
        // costs download, and a rehearsal is not the night to spend it.
        if (readDataSubset is not null)
        {
            log.Skipped($"Would read {readDataSubset} of {repository.Location}'s pack data back; checking its structure only.");
        }

        await commands.Run(ResticClient.CheckCommand(repository, readDataSubset: null), ct);
    }
}
