namespace Wolfe.Lab.Build.Workflows.DotNetTool.Models;

/// <summary>
/// The NuGet feed a tool publishes to.
/// </summary>
public sealed record FeedSettings
{
    /// <summary>
    /// The feed's service index.
    /// </summary>
    public Uri? Source { get; init; }
}
