using Wolfe.Lab.Build.Clients.Secrets;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Obsidian.Models;

/// <summary>
/// How commits reach Forgejo.
/// </summary>
public sealed record PushSettings
{
    /// <summary>
    /// The account the token belongs to.
    /// </summary>
    public GitUsername? Username { get; init; }

    /// <summary>
    /// Where the access token is.
    /// </summary>
    public SecretReference? Token { get; init; }
}
