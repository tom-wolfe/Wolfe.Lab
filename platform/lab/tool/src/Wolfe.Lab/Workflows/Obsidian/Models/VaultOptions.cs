using Wolfe.Lab.Values;

namespace Wolfe.Lab.Workflows.Obsidian.Models;

/// <summary>
/// One vault.
/// </summary>
public sealed record VaultOptions
{
    /// <summary>
    /// The checkout on the node; <c>~</c> stands for the home directory.
    /// </summary>
    public HostPath? Path { get; init; }

    /// <summary>
    /// The Forgejo repository the checkout pushes to.
    /// </summary>
    public RepositoryUrl? Repository { get; init; }
}
