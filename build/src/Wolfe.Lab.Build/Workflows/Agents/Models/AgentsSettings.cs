using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Agents.Models;

/// <summary>
/// The shape of an agents component's <c>ritten.json</c>: <c>"workflow": "agents"</c>.
/// </summary>
public sealed record AgentsSettings : WorkflowSettings
{
    /// <summary>
    /// What each node runs, by the node's name — the same name its runner carries.
    /// </summary>
    public Dictionary<string, NodeAgentsSettings> Nodes { get; init; } = [];
}

/// <summary>
/// One node's agents.
/// </summary>
public sealed record NodeAgentsSettings
{
    /// <summary>
    /// External volumes the agents read, which must be mounted before they are converged.
    /// </summary>
    public IReadOnlyList<HostPath> Volumes { get; init; } = [];

    /// <summary>
    /// The agents, by the name each is labelled after.
    /// </summary>
    public Dictionary<string, AgentSettings> Agents { get; init; } = [];
}
