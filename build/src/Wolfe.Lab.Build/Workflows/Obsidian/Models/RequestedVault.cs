namespace Wolfe.Lab.Build.Workflows.Obsidian.Models;

/// <summary>
/// The vault the invocation asked for.
/// </summary>
/// <param name="Name">The name given to <c>--vault</c>.</param>
public sealed record RequestedVault(string Name);
