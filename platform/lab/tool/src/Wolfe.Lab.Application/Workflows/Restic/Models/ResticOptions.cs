using Wolfe.Lab.Infrastructure.Heartbeat;

namespace Wolfe.Lab.Application.Workflows.Restic.Models;

/// <summary>
/// The shape of <c>platform/restic/ritten.json</c>: the retention policy, the verify sample, and the
/// dead man's switch the offsite copy reports to.
/// </summary>
public sealed record ResticOptions : WorkflowSettings
{
    /// <summary>
    /// What the nightly prune keeps.
    /// </summary>
    public RetentionOptions Retention { get; init; } = new();

    /// <summary>
    /// How much of the offsite copy the weekly check reads back.
    /// </summary>
    public VerifyOptions Verify { get; init; } = new();

    /// <summary>
    /// The check pinged after a green offsite copy.
    /// </summary>
    public HeartbeatCheckOptions Heartbeat { get; init; } = new();
}
