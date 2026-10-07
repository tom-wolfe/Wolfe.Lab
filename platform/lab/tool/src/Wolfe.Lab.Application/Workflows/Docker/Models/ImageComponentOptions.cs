namespace Wolfe.Lab.Application.Workflows.Docker.Models;

/// <summary>
/// The shape of an image component's <c>ritten.json</c>: <c>"workflow": "image"</c>.
/// </summary>
public sealed record ImageComponentOptions : WorkflowSettings
{
    /// <summary>
    /// The images it builds and pushes.
    /// </summary>
    public IReadOnlyList<ImageOptions> Images { get; init; } = [];
}
