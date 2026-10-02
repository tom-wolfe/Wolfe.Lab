using Wolfe.Lab.Domain.Paths;
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
    /// The name the component is installed under on the node.
    /// </summary>
    /// <remarks>
    /// Components are installed by name into one flat root, and every service's compose component
    /// is called <c>compose</c> — so each names its release, and names it after its service.
    /// </remarks>
    public string? Release { get; init; }

    /// <summary>
    /// External volumes the stack binds.
    /// </summary>
    public IReadOnlyList<HostPath> Volumes { get; init; } = [];

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
    /// Directories published to the node beside the release, which is itself one.
    /// </summary>
    public IReadOnlyList<ArtifactOptions> Artifacts { get; init; } = [];
}
