using Wolfe.Lab.Clients.Releases;

namespace Wolfe.Lab.Workflows.Agents.Models;

/// <summary>
/// The shape of an agents component's <c>ritten.json</c>: <c>"workflow": "agents"</c>.
/// </summary>
public sealed record AgentsOptions : WorkflowSettings
{
    /// <summary>
    /// What each node runs, by the node's name — the same name its runner carries.
    /// </summary>
    public Dictionary<string, NodeAgentsOptions> Nodes { get; init; } = [];

    /// <summary>
    /// Directories of the component published to every node it runs on.
    /// </summary>
    public IReadOnlyList<ArtifactOptions> Artifacts { get; init; } = [];
}
