namespace Wolfe.Lab.Build.Clients.Releases;

/// <summary>
/// The artifacts as published, and when their content last changed on this node.
/// </summary>
/// <param name="Items">The artifacts.</param>
/// <param name="Stamp">
/// The newest write under any of their outputs, or null when there are none. The installer only
/// writes a file whose content changed, so this moves exactly when a running process would need
/// to read its files again — and it is read from the node, so a restart that failed is still
/// owed the next time.
/// </param>
public sealed record PublishedArtifacts(IReadOnlyList<Artifact> Items, DateTimeOffset? Stamp);
