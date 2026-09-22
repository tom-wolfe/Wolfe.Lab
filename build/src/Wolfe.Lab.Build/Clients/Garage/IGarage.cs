namespace Wolfe.Lab.Build.Clients.Garage;

/// <summary>
/// The running Garage, through its own CLI inside the container.
/// </summary>
public interface IGarage
{
    /// <summary>
    /// Whether the daemon answers yet.
    /// </summary>
    Task<bool> IsReady(CancellationToken ct = default);

    /// <summary>
    /// The current cluster layout version; zero before any layout has been applied.
    /// </summary>
    Task<int> LayoutVersion(CancellationToken ct = default);

    /// <summary>
    /// This node's id, as the layout commands take it.
    /// </summary>
    Task<string> NodeId(CancellationToken ct = default);

    /// <summary>
    /// Stages a role for the node: which zone it is in, how much it stores.
    /// </summary>
    Task AssignLayout(string nodeId, string zone, string capacity, CancellationToken ct = default);

    /// <summary>
    /// Applies the staged layout as the given version.
    /// </summary>
    Task ApplyLayout(int version, CancellationToken ct = default);
}
