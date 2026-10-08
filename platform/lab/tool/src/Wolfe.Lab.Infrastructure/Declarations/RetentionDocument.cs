using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// What a prune keeps.
/// </summary>
[AdditionalProperties(false)]
internal sealed record RetentionDocument
{
    [Required, Minimum(0), Description("The most recent daily snapshots kept, per group.")]
    public int Daily { get; init; }

    [Required, Minimum(0), Description("The most recent weekly snapshots kept, per group.")]
    public int Weekly { get; init; }

    [Required, Minimum(0), Description("The most recent monthly snapshots kept, per group.")]
    public int Monthly { get; init; }

    [UniqueItems(true), Description("Tags whose snapshots are kept forever: the labelled dumps taken before an upgrade.")]
    public List<string>? KeepTags { get; init; }
}
