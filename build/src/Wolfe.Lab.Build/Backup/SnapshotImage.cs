namespace Wolfe.Lab.Build.Backup;

/// <summary>
/// The image the snapshot is tagged with, so a restore pairs with the version that wrote it —
/// these schemas migrate forward only.
/// </summary>
/// <param name="Tag">The image reference, or null when no container was involved.</param>
public sealed record SnapshotImage(string? Tag)
{
    /// <summary>
    /// A warm snapshot's: nothing ran, nothing to pair with.
    /// </summary>
    public static SnapshotImage None { get; } = new(Tag: null);
}
