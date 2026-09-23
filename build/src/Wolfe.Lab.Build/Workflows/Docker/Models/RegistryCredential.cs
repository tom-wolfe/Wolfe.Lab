using Wolfe.Lab.Build.Clients.Secrets;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Docker.Models;

/// <summary>
/// What a registry login authenticates with: the token stays a reference until the login.
/// </summary>
/// <param name="Username">The account the token belongs to.</param>
/// <param name="Token">Where the token is.</param>
public sealed record RegistryCredential(GitUsername Username, SecretReference Token);
