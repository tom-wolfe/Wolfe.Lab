namespace Wolfe.Lab.Build.Workflows.Obsidian.Models;

/// <summary>
/// Every vault the slice declares.
/// </summary>
/// <param name="ByName">The vaults, keyed as <c>ritten.json</c> names them.</param>
public sealed record KnownVaults(IReadOnlyDictionary<string, Vault> ByName);
