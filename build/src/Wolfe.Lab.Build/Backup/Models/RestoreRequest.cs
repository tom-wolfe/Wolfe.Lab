namespace Wolfe.Lab.Build.Backup.Models;

/// <summary>
/// What a restore was asked for.
/// </summary>
/// <param name="SnapshotId">A specific snapshot, or null for the latest.</param>
/// <param name="AnyImage">Whether to restore onto an image other than the one the snapshot was taken under.</param>
public sealed record RestoreRequest(string? SnapshotId, bool AnyImage);
