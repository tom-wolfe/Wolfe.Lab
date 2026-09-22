using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Workflows.Docker.Models;

/// <summary>
/// The shape of a docker component's <c>ritten.json</c>: <c>"workflow": "docker"</c>.
/// </summary>
/// <remarks>
/// A component, not a slice. A compose project is a regular shape — the same jobs every time —
/// where a slice is a unique recombination of components and needs a workflow of its own.
/// </remarks>
public record DockerSettings : SliceSettings
{
    /// <summary>
    /// What to install the component as, when the directory's own name would not do.
    /// </summary>
    /// <remarks>
    /// Components are installed by name into one flat root, and every slice's compose
    /// component is called <c>compose</c> — so without this they would all be the same
    /// directory. Naming it after the slice keeps the release path what it has always been.
    /// </remarks>
    public string? Release { get; init; }

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
