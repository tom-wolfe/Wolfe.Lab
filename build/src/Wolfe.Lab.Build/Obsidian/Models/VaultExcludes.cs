namespace Wolfe.Lab.Build.Obsidian.Models;

/// <summary>
/// What git leaves out of a vault.
/// </summary>
/// <param name="Patterns">Patterns in <c>.gitignore</c> syntax.</param>
public sealed record VaultExcludes(IReadOnlyList<string> Patterns);
