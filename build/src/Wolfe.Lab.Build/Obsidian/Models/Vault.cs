using Wolfe.Lab.Build.Git;

namespace Wolfe.Lab.Build.Obsidian.Models;

/// <summary>
/// A vault the node mirrors: its checkout and where the checkout pushes.
/// </summary>
/// <param name="Name">The name the invocation asked for.</param>
/// <param name="Directory">The checkout.</param>
/// <param name="Repository">The Forgejo repository's URL.</param>
public sealed record Vault(string Name, IDirectory Directory, RepositoryUrl Repository);
