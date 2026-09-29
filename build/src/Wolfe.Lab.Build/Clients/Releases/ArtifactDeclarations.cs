namespace Wolfe.Lab.Build.Clients.Releases;

/// <summary>
/// The artifacts a job publishes: what the component declared, and what the workflow adds —
/// compose publishes the component itself as its release.
/// </summary>
/// <param name="Artifacts">The declarations.</param>
public sealed record ArtifactDeclarations(IReadOnlyList<ArtifactOptions> Artifacts);
