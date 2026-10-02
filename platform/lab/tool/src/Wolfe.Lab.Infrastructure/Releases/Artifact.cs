namespace Wolfe.Lab.Infrastructure.Releases;

/// <summary>
/// An artifact as the steps consume it.
/// </summary>
/// <param name="Source">The directory in the checkout.</param>
/// <param name="Output">The directory on the node.</param>
public sealed record Artifact(IDirectory Source, IDirectory Output);
