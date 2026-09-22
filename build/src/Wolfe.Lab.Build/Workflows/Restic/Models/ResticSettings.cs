using Wolfe.Lab.Build.Clients.Heartbeat;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Workflows.Restic.Models;

/// <summary>
/// The shape of <c>restic/ritten.json</c>: the retention policy, the verify sample, and the
/// dead man's switch the offsite copy reports to.
/// </summary>
public sealed record ResticSettings : SliceSettings
{
    /// <summary>
    /// What the nightly prune keeps.
    /// </summary>
    public RetentionSettings Retention { get; init; } = new();

    /// <summary>
    /// How much of the offsite copy the weekly check reads back.
    /// </summary>
    public VerifySettings Verify { get; init; } = new();

    /// <summary>
    /// The check pinged after a green offsite copy.
    /// </summary>
    public HeartbeatCheckSettings Heartbeat { get; init; } = new();
}
