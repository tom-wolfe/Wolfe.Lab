using Wolfe.Lab.Domain.Network;

namespace Wolfe.Lab.Domain.Catalog.Facets.Telemetry;

/// <summary>
/// Where a service serves its Prometheus metrics, in its own container.
/// </summary>
/// <param name="Port">The container's port, as the service listens on it.</param>
/// <param name="Path">The path it serves them on.</param>
public sealed record MetricsEndpoint(Port Port, HttpPath Path)
{
    /// <summary>
    /// The node's port it is published on.
    /// </summary>
    public Port? Published { get; init; }
}
