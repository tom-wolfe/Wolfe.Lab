using Wolfe.Lab.Build.Clients.Secrets;

namespace Wolfe.Lab.Build.Workflows.DotNetTool.Models;

/// <summary>
/// A NuGet feed and the credential that publishes to it.
/// </summary>
public sealed record FeedSettings
{
    /// <summary>
    /// The feed's service index.
    /// </summary>
    public Uri? Source { get; init; }

    /// <summary>
    /// Where the token that may publish to it is.
    /// </summary>
    public SecretReference? Token { get; init; }
}
