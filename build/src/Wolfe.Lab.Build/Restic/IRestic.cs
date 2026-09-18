using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Restic.Models;

namespace Wolfe.Lab.Build.Restic;

/// <summary>
/// The lab's backup tool.
/// </summary>
public interface IRestic
{
    /// <summary>
    /// Snapshots the paths into the repository.
    /// </summary>
    /// <param name="repository">The repository written to.</param>
    /// <param name="paths">The directories to snapshot.</param>
    /// <param name="excludes">Absolute paths left out.</param>
    /// <param name="tags">Tags on the snapshot.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task<Snapshot> Backup(ResticRepository repository, IReadOnlyList<IDirectory> paths, IReadOnlyList<string> excludes, IReadOnlyList<string> tags, CancellationToken ct = default);

    /// <summary>
    /// Drops one snapshot. The data it referenced is rewritten out by the nightly prune.
    /// </summary>
    /// <param name="repository">The repository holding it.</param>
    /// <param name="snapshot">The snapshot to drop.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task Forget(ResticRepository repository, Snapshot snapshot, CancellationToken ct = default);

    /// <summary>
    /// Ships every snapshot the offsite repository does not have yet. Idempotent.
    /// </summary>
    /// <param name="offsite">The destination, whose environment names the source.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task Copy(OffsiteRepository offsite, CancellationToken ct = default);

    /// <summary>
    /// Applies the retention policy and rewrites out what it unreferenced.
    /// </summary>
    /// <param name="repository">The repository pruned.</param>
    /// <param name="policy">What is kept.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task Prune(ResticRepository repository, RetentionPolicy policy, CancellationToken ct = default);

    /// <summary>
    /// Checks the repository's structure, and optionally reads a share of its pack data back
    /// against the index.
    /// </summary>
    /// <param name="repository">The repository checked.</param>
    /// <param name="readDataSubset">The share of pack data read, as restic spells it, or null for a structural check only.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task Check(ResticRepository repository, string? readDataSubset, CancellationToken ct = default);
}
