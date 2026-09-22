namespace Wolfe.Lab.Build.Agents;

/// <summary>
/// The agents a converge will make true, resolved against this node.
/// </summary>
/// <param name="Agents">The agents, in a stable order.</param>
public sealed record AgentPlan(IReadOnlyList<AgentDefinition> Agents);
