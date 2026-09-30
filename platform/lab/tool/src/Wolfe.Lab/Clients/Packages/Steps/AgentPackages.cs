namespace Wolfe.Lab.Clients.Packages.Steps;

/// <summary>
/// The package each agent that declares one runs from, by the agent's name.
/// </summary>
public sealed record AgentPackages(IReadOnlyDictionary<string, InstalledPackage> Packages);
