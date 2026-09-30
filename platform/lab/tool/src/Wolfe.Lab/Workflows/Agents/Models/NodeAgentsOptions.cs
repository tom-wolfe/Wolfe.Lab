using Wolfe.Lab.Clients.Agents;
using Wolfe.Lab.Values;

namespace Wolfe.Lab.Workflows.Agents.Models;

/// <summary>
/// One node's agents.
/// </summary>
public sealed record NodeAgentsOptions
{
    /// <summary>
    /// External volumes the agents read, which must be mounted before they are converged.
    /// </summary>
    public IReadOnlyList<HostPath> Volumes { get; init; } = [];

    /// <summary>
    /// The agents, by the name each is labelled after.
    /// </summary>
    public Dictionary<string, AgentOptions> Agents { get; init; } = [];
}
