namespace Wolfe.Lab.Build.Clients.Garage;

/// <summary>
/// Reads the cluster; stages and applies nothing.
/// </summary>
internal sealed class DryRunGarage(IWorkflowLog log, GarageClient inner) : IGarage
{
    /// <inheritdoc />
    public Task<bool> IsReady(CancellationToken ct = default) => inner.IsReady(ct);

    /// <inheritdoc />
    public Task<int> LayoutVersion(CancellationToken ct = default) => inner.LayoutVersion(ct);

    /// <inheritdoc />
    public Task<string> NodeId(CancellationToken ct = default) => inner.NodeId(ct);

    /// <inheritdoc />
    public Task AssignLayout(string nodeId, string zone, string capacity, CancellationToken ct = default)
    {
        log.Skipped($"Would assign node {nodeId} to zone {zone} with capacity {capacity}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ApplyLayout(int version, CancellationToken ct = default)
    {
        log.Skipped($"Would apply layout version {version}.");
        return Task.CompletedTask;
    }
}
