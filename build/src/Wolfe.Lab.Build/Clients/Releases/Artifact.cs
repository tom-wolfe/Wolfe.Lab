namespace Wolfe.Lab.Build.Clients.Releases;

/// <summary>
/// One entry of a component's <c>artifacts</c>: a directory of the component, published to the node.
/// </summary>
public sealed record ArtifactSettings
{
    /// <summary>
    /// The directory to publish, relative to the component.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Where it is published: a directory inside <c>${LAB_ROOT}</c>, which it is written as.
    /// </summary>
    public string? Output { get; init; }
}

/// <summary>
/// The artifacts a job publishes: what the component declared, and what the workflow adds —
/// compose publishes the component itself as its release.
/// </summary>
/// <param name="Artifacts">The declarations.</param>
public sealed record ArtifactDeclarations(IReadOnlyList<ArtifactSettings> Artifacts);

/// <summary>
/// An artifact as the steps consume it.
/// </summary>
/// <param name="Source">The directory in the checkout.</param>
/// <param name="Output">The directory on the node.</param>
public sealed record Artifact(IDirectory Source, IDirectory Output);

/// <summary>
/// The artifacts a job resolved, before they are published.
/// </summary>
public sealed record Artifacts(IReadOnlyList<Artifact> Items);

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
