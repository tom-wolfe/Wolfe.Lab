using Wolfe.Lab.Domain.Git;
using Wolfe.Lab.Infrastructure.Secrets;

namespace Wolfe.Lab.Application.Workflows.Obsidian.Models;

/// <summary>
/// How commits reach Forgejo.
/// </summary>
public sealed record PushOptions
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
