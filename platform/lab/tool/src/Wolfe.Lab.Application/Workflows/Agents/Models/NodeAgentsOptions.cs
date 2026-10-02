using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;

namespace Wolfe.Lab.Application.Workflows.Agents.Models;

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
