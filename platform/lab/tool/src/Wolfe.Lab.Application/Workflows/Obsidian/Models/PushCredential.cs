using Wolfe.Lab.Domain.Git;
using Wolfe.Lab.Domain.Secrets;

namespace Wolfe.Lab.Application.Workflows.Obsidian.Models;

/// <summary>
/// What a push authenticates with: the token stays a reference until the moment of the push.
/// </summary>
/// <param name="Username">The account the token belongs to.</param>
/// <param name="Token">Where the token is.</param>
public sealed record PushCredential(GitUsername Username, SecretReference Token);
