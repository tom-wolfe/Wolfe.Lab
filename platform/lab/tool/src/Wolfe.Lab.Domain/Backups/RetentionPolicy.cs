using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Domain.Backups;

/// <summary>
/// What a prune keeps. One policy, both repositories.
/// </summary>
public sealed record RetentionPolicy
{
    private RetentionPolicy() { }

    /// <summary>
    /// The most recent daily snapshots kept, per group.
    /// </summary>
    public required SnapshotCount Daily { get; init; }

    /// <summary>
    /// The most recent weekly snapshots kept, per group.
    /// </summary>
    public required SnapshotCount Weekly { get; init; }

    /// <summary>
    /// The most recent monthly snapshots kept, per group.
    /// </summary>
    public required SnapshotCount Monthly { get; init; }

    /// <summary>
    /// Tags whose snapshots are kept forever: the labelled dumps taken before an upgrade.
    /// </summary>
    public IReadOnlyList<string> KeepTags { get; init; } = [];

    /// <summary>
    /// A policy keeping these snapshots, when it keeps any: one that keeps none prunes everything.
    /// </summary>
    public static Result<RetentionPolicy> Create(SnapshotCount daily, SnapshotCount weekly, SnapshotCount monthly) =>
        daily.Value + weekly.Value + monthly.Value > 0
            ? new RetentionPolicy { Daily = daily, Weekly = weekly, Monthly = monthly }
            : new FieldError("retention", new Error("keeps nothing: every snapshot would be pruned."));
}
