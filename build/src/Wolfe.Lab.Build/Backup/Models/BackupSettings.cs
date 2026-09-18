using Wolfe.Lab.Build.Paths;

namespace Wolfe.Lab.Build.Backup.Models;

/// <summary>
/// The <c>backup</c> section of a slice's <c>ritten.json</c>: what a snapshot holds, and what
/// has to be quiet while it is taken.
/// </summary>
public sealed record BackupSettings
{
    /// <summary>
    /// The directories to snapshot.
    /// </summary>
    public IReadOnlyList<HostPath> Paths { get; init; } = [];

    /// <summary>
    /// Paths under those directories that restic leaves out: caches, logs, anything the
    /// service regenerates.
    /// </summary>
    public IReadOnlyList<HostPath> Excludes { get; init; } = [];

    /// <summary>
    /// The container whose stack is stopped for the snapshot, and whose image tags it. Absent
    /// for a warm snapshot: files nothing is writing, or a service whose database is its own dump.
    /// </summary>
    public string? Stop { get; init; }

    /// <summary>
    /// The container whose image tags the snapshot when it is not the one stopped: a warm
    /// snapshot of a service still pairs a restore with the version that wrote it. Defaults to
    /// <see cref="Stop"/>.
    /// </summary>
    public string? Image { get; init; }
}
