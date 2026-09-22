using Wolfe.Lab.Build.Clients.Heartbeat;

namespace Wolfe.Lab.Build.Workflows.Heartbeat.Models;

/// <summary>
/// What <c>heartbeat/ritten.json</c> declares: the check the ping lands on, in the same
/// <c>heartbeat</c> section any watched job carries.
/// </summary>
public sealed record HeartbeatSettings : WorkflowSettings
{
    /// <summary>
    /// The check's slug and the project ping key.
    /// </summary>
    public HeartbeatCheckSettings Heartbeat { get; init; } = new();
}
