namespace Wolfe.Lab.Clients.Releases;

/// <summary>
/// One entry of a component's <c>artifacts</c>: a directory of the component, published to the node.
/// </summary>
public sealed record ArtifactOptions
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
