
namespace Wolfe.Lab.Application.Workflows.Docker.Models;

/// <summary>
/// An image this component builds.
/// </summary>
public sealed record ImageOptions
{
    /// <summary>The tag the compose file refers to.</summary>
    public string? Tag { get; init; }

    /// <summary>The build context, relative to the component.</summary>
    public string? Context { get; init; }

    /// <summary>
    /// The Dockerfile, relative to the context, when it is not the context's own <c>Dockerfile</c>.
    /// </summary>
    public string? Dockerfile { get; init; }
}
