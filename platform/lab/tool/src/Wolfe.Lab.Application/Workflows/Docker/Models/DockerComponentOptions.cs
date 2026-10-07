using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.Docker.Models;

/// <summary>
/// The shape of a docker component's <c>ritten.json</c>: <c>"workflow": "docker"</c>.
/// </summary>
/// <remarks>
/// A component, not a service. A compose project is a regular shape — the same jobs every time —
/// and a service is a directory of such components, each with a declaration of its own.
/// </remarks>
public record DockerComponentOptions : WorkflowSettings
{
    /// <summary>
    /// Images built from source before the stack is converged.
    /// </summary>
    /// <remarks>
    /// Declared rather than read out of the compose file's <c>build:</c> stanzas, because
    /// compose only builds an image it cannot find — so a source change would deploy the
    /// previous one and say nothing.
    /// </remarks>
    public IReadOnlyList<ImageOptions> Images { get; init; } = [];

    /// <summary>
    /// Directories published to the node beside the stack, which is itself installed as one.
    /// </summary>
    public IReadOnlyList<ArtifactOptions> Artifacts { get; init; } = [];
}
