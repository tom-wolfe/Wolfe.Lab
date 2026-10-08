using Wolfe.Lab.Domain.Catalog.Components.Garage;

namespace Wolfe.Lab.Infrastructure.Garage;

/// <summary>
/// Reads the cluster; stages and applies nothing.
/// </summary>
internal sealed class DryRunGarage(IWorkflowLog log, GarageClient inner) : IGarage
{
    /// <inheritdoc />
    public Task<bool> AwaitReady(CancellationToken ct = default) => inner.AwaitReady(ct);

    /// <inheritdoc />
    public Task<ClusterLayout> Layout(CancellationToken ct = default) => inner.Layout(ct);

    /// <inheritdoc />
    public Task<string> NodeId(CancellationToken ct = default) => inner.NodeId(ct);

    /// <inheritdoc />
    public Task AssignLayout(string nodeId, GarageLayout layout, CancellationToken ct = default)
    {
        log.Skipped($"Would assign node {nodeId} to zone {layout.Zone.Value} with a capacity of {layout.Capacity.Value} bytes.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ApplyLayout(int version, CancellationToken ct = default)
    {
        log.Skipped($"Would apply layout version {version}.");
        return Task.CompletedTask;
    }
}
