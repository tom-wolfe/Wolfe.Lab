using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog.Components.Backups;

/// <summary>
/// The well-known problems with a backup's declaration.
/// </summary>
public static class BackupErrors
{
    /// <summary>
    /// It names no directory to snapshot.
    /// </summary>
    public static Error NothingToSnapshot { get; } = new("names nothing to snapshot.");

    /// <summary>
    /// A path it leaves out or verifies is in none of the directories it snapshots.
    /// </summary>
    public static Error OutsideThePaths(HostPath path) => new($"'{path}' is in none of the paths the snapshot holds.");

    /// <summary>
    /// It is warm, but part of nothing that could have been stopped.
    /// </summary>
    public static Error WarmOfNothing { get; } = new("only a backup part of something that runs is taken warm, while it runs; one part of nothing is never stopped anyway.");
}
