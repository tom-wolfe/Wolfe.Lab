namespace Wolfe.Lab.Build.Clients.Agents;

/// <summary>
/// One environment variable, escaped.
/// </summary>
/// <param name="Key">The name.</param>
/// <param name="Value">The value.</param>
public sealed record UnitVariable(string Key, string Value);
