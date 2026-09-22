namespace Wolfe.Lab.Build.Workflows.Docker.Models;

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
