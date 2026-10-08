using Wolfe.Lab.Domain.Git;

namespace Wolfe.Lab.Application.Workflows.Obsidian.Models;

/// <summary>
/// A vault, as the steps sync it.
/// </summary>
/// <param name="Name">Its name.</param>
/// <param name="Directory">Where it is checked out.</param>
/// <param name="Repository">Where it is pushed.</param>
/// <param name="Push">What the push authenticates with.</param>
/// <param name="Excludes">What a commit leaves out.</param>
public sealed record Vault(string Name, IDirectory Directory, RepositoryUrl Repository, PushCredential Push, IReadOnlyList<string> Excludes);
