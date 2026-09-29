using Wolfe.Lab.Build.Clients.Releases;

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

    /// <summary>
    /// Directories of the component published to every node it runs on.
    /// </summary>
    public IReadOnlyList<ArtifactSettings> Artifacts { get; init; } = [];
}
