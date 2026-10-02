namespace Wolfe.Lab.Application.Workflows.DotNetTool.Models;

/// <summary>
/// The NuGet feed a tool publishes to.
/// </summary>
public sealed record FeedOptions
{
    /// <summary>
    /// The feed's service index.
    /// </summary>
    public Uri? Source { get; init; }
}
