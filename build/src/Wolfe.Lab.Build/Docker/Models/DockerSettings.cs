using Wolfe.Lab.Build.Deploy.Models;

namespace Wolfe.Lab.Build.Docker.Models;

/// <summary>
/// The shape of a docker component's <c>ritten.json</c>: <c>"workflow": "docker"</c>.
/// </summary>
/// <remarks>
/// A component, not a slice. A compose project is a regular shape — the same jobs every time —
/// where a slice is a unique recombination of components and needs a workflow of its own.
/// </remarks>
public sealed record DockerSettings : SliceSettings
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

/// <summary>
/// An image this component builds.
/// </summary>
public sealed record ImageSettings
{
    /// <summary>The tag the compose file refers to.</summary>
    public string? Tag { get; init; }

    /// <summary>The build context, relative to the component.</summary>
    public string? Context { get; init; }

    /// <summary>The image as the steps consume it, or null while either half is missing.</summary>
    public BuildableImage? ToImage() =>
        Tag is { Length: > 0 } tag && Context is { Length: > 0 } context ? new BuildableImage(tag, context) : null;
}

/// <summary>
/// An image the deploy will build.
/// </summary>
/// <param name="Tag">The tag to give it.</param>
/// <param name="Context">The build context, relative to the component.</param>
public sealed record BuildableImage(string Tag, string Context);

/// <summary>
/// The images a deploy will build, in the order declared.
/// </summary>
/// <param name="Images">The images.</param>
public sealed record ImagePlan(IReadOnlyList<BuildableImage> Images);
