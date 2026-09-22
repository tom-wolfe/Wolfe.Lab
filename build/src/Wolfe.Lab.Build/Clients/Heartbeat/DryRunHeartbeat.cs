namespace Wolfe.Lab.Build.Clients.Heartbeat;

/// <summary>
/// Says which check would have been pinged. A rehearsal must never tell the switch the real job
/// ran.
/// </summary>
internal sealed class DryRunHeartbeat(IWorkflowLog log) : IHeartbeat
{
    /// <inheritdoc />
    public Task Ping(HeartbeatCheck check, CancellationToken ct = default)
    {
        log.Skipped($"Would ping {check.Slug}.");
        return Task.CompletedTask;
    }
}
