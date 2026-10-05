using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.Agents.Models;

/// <summary>
/// The shape of an agents component's <c>ritten.json</c>: <c>"workflow": "agents"</c>.
/// </summary>
public sealed record AgentsOptions : WorkflowSettings
{
    /// <summary>
    /// Directories of the component published to every node it runs on.
    /// </summary>
    public IReadOnlyList<ArtifactOptions> Artifacts { get; init; } = [];
}
