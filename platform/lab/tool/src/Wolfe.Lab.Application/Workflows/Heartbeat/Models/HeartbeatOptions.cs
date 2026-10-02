using Wolfe.Lab.Infrastructure.Heartbeat;

namespace Wolfe.Lab.Application.Workflows.Heartbeat.Models;

/// <summary>
/// What <c>heartbeat/ritten.json</c> declares: the check the ping lands on, in the same
/// <c>heartbeat</c> section any watched job carries.
/// </summary>
public sealed record HeartbeatOptions : WorkflowSettings
{
    /// <summary>
    /// The check's slug and the project ping key.
    /// </summary>
    public HeartbeatCheckOptions Heartbeat { get; init; } = new();
}
