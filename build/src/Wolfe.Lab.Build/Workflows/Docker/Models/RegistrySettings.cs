using Wolfe.Lab.Build.Clients.Secrets;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Docker.Models;

/// <summary>
/// The registry an image component pushes to. The host is the one in each image's tag.
/// </summary>
public sealed record RegistrySettings
{
    /// <summary>
    /// The account the token belongs to.
    /// </summary>
    public GitUsername? Username { get; init; }

    /// <summary>
    /// Where the token that may push is.
    /// </summary>
    public SecretReference? Token { get; init; }
}
