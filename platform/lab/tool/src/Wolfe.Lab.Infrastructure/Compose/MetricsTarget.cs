using Wolfe.Lab.Domain.Network;

namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// One of a component's metrics endpoints, as the node's collector reaches it.
/// </summary>
/// <param name="Port">The node's port it answers on: its own, published, or the container's on the host's network.</param>
/// <param name="Path">The path it serves them on.</param>
public sealed record MetricsTarget(Port Port, HttpPath Path);
