using Wolfe.Lab.Build.Git;

namespace Wolfe.Lab.Build.Obsidian.Models;

/// <summary>
/// One vault.
/// </summary>
public sealed record VaultSettings
{
    /// <summary>
    /// The checkout on the node; <c>~</c> stands for the home directory.
    /// </summary>
    public VaultPath? Path { get; init; }

    /// <summary>
    /// The Forgejo repository the checkout pushes to.
    /// </summary>
    public RepositoryUrl? Repository { get; init; }
}
