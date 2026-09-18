namespace Wolfe.Lab.Build.Restic.Models;

/// <summary>
/// What the nightly prune keeps, applied to both repositories identically.
/// </summary>
public sealed record RetentionSettings
{
    /// <summary>
    /// The most recent daily snapshots kept, per group.
    /// </summary>
    public int Daily { get; init; } = 7;

    /// <summary>
    /// The most recent weekly snapshots kept, per group.
    /// </summary>
    public int Weekly { get; init; } = 5;

    /// <summary>
    /// The most recent monthly snapshots kept, per group.
    /// </summary>
    public int Monthly { get; init; } = 12;

    /// <summary>
    /// Tags whose snapshots are kept forever: the labelled dumps taken before an upgrade.
    /// </summary>
    public IReadOnlyList<string> KeepTags { get; init; } = [];
}
