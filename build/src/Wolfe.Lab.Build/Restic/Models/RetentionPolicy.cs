namespace Wolfe.Lab.Build.Restic.Models;

/// <summary>
/// What a prune keeps. One policy, both repositories.
/// </summary>
/// <param name="Daily">The most recent daily snapshots kept, per group.</param>
/// <param name="Weekly">The most recent weekly snapshots kept, per group.</param>
/// <param name="Monthly">The most recent monthly snapshots kept, per group.</param>
/// <param name="KeepTags">Tags whose snapshots are kept forever.</param>
public sealed record RetentionPolicy(int Daily, int Weekly, int Monthly, IReadOnlyList<string> KeepTags);