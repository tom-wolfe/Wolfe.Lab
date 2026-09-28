using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Docker.Models;

/// <summary>
/// The shape of a docker component's <c>ritten.json</c>: <c>"workflow": "docker"</c>.
/// </summary>
/// <remarks>
/// A component, not a slice. A compose project is a regular shape — the same jobs every time —
/// and a slice is a directory of such components, each with a declaration of its own.
/// </remarks>
public record DockerSettings : WorkflowSettings
{
    /// <summary>
    /// The name the component is installed under on the node.
    /// </summary>
    /// <remarks>
    /// Components are installed by name into one flat root, and every slice's compose component
    /// is called <c>compose</c> — so each names its release, and names it after its slice.
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
    public IReadOnlyList<ImageSettings> Images { get; init; } = [];
}
