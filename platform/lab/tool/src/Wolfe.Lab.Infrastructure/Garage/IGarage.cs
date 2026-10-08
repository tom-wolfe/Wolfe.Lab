using Wolfe.Lab.Domain.Catalog.Components.Garage;

namespace Wolfe.Lab.Infrastructure.Garage;

/// <summary>
/// The running Garage, through its own CLI inside the container.
/// </summary>
public interface IGarage
{
    /// <summary>
    /// Waits for the daemon to answer.
    /// </summary>
    Task<bool> AwaitReady(CancellationToken ct = default);

    /// <summary>
    /// The cluster's current layout.
    /// </summary>
    Task<ClusterLayout> Layout(CancellationToken ct = default);

    /// <summary>
    /// This node's id.
    /// </summary>
    Task<string> NodeId(CancellationToken ct = default);

    /// <summary>
    /// Stages a role for the node: which zone it is in, how much it stores.
    /// </summary>
    Task AssignLayout(string nodeId, GarageLayout layout, CancellationToken ct = default);

    /// <summary>
    /// Applies the staged layout as the given version.
    /// </summary>
    Task ApplyLayout(int version, CancellationToken ct = default);
}
