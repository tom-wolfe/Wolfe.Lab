namespace Wolfe.Lab.Build.Backup.Models;

/// <summary>
/// What one slice's snapshot holds, and what has to be quiet while it is taken.
/// </summary>
/// <param name="Paths">The directories to snapshot.</param>
/// <param name="Excludes">Absolute paths restic leaves out.</param>
/// <param name="Container">The container whose stack is stopped for the snapshot, or null for a warm one.</param>
/// <param name="Image">The container whose image tags the snapshot, or null for none.</param>
/// <param name="Verify">Absolute paths a restore must bring back non-empty.</param>
public sealed record BackupPlan(IReadOnlyList<IDirectory> Paths, IReadOnlyList<string> Excludes, string? Container, string? Image, IReadOnlyList<string> Verify);
