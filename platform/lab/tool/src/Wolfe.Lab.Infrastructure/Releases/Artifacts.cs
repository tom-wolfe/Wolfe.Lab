namespace Wolfe.Lab.Infrastructure.Releases;

/// <summary>
/// The artifacts a job resolved, before they are published.
/// </summary>
public sealed record Artifacts(IReadOnlyList<Artifact> Items);
