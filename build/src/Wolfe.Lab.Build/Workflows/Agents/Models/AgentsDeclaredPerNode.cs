namespace Wolfe.Lab.Build.Workflows.Agents.Models;

/// <summary>
/// Every node's declarations, as the check judges them.
/// </summary>
/// <param name="Nodes">The nodes, by name.</param>
public sealed record AgentsDeclaredPerNode(IReadOnlyDictionary<string, NodeAgentsSettings> Nodes);
