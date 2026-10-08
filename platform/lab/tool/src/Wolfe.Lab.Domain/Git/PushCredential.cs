using Wolfe.Lab.Domain.Secrets;

namespace Wolfe.Lab.Domain.Git;

/// <summary>
/// Who a push authenticates as, and where its token is kept.
/// </summary>
/// <param name="Username">The account it pushes as.</param>
/// <param name="Token">The token it pushes with.</param>
public sealed record PushCredential(GitUsername Username, SecretReference Token);
