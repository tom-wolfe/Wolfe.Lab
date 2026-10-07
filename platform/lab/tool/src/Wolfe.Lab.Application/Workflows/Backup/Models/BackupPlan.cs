namespace Wolfe.Lab.Application.Workflows.Backup.Models;

/// <summary>
/// What one backup's snapshot holds, and what has to be quiet while it is taken.
/// </summary>
/// <param name="Service">The service whose state it is: what its snapshots are filed under.</param>
/// <param name="Paths">The directories to snapshot.</param>
/// <param name="Excludes">Absolute paths restic leaves out.</param>
/// <param name="Verify">Absolute paths a restore must bring back non-empty.</param>
/// <param name="Stack">The installed stack stopped for the snapshot, or null for a warm one.</param>
/// <param name="Container">The container that stack runs, which must still be stopped when the snapshot is done; null with no stack.</param>
/// <param name="Image">The container whose image tags the snapshot, or null for none.</param>
public sealed record BackupPlan(string Service, IReadOnlyList<IDirectory> Paths, IReadOnlyList<string> Excludes, IReadOnlyList<string> Verify,
    IDirectory? Stack, string? Container, string? Image)
{
    /// <summary>
    /// The tag every snapshot of the service carries: kept from the first, so a restore finds
    /// all of its history.
    /// </summary>
    public string Tag => $"service:{Service}";
}
