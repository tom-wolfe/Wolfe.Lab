namespace Wolfe.Lab.Build.Clients.Alerts;

/// <summary>
/// Says what would have been sent. Nothing reads from the transport, so the rehearsal replaces it.
/// </summary>
internal sealed class DryRunAlerts(IWorkflowLog log) : IAlerts
{
    /// <inheritdoc />
    public Task Send(Alert alert, CancellationToken ct = default)
    {
        log.Skipped($"Would alert: {alert.Title}");
        return Task.CompletedTask;
    }
}
