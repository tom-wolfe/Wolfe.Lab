using Wolfe.Lab.Domain.Catalog.Facets.Heartbeats;

namespace Wolfe.Lab.Infrastructure.Heartbeat;

/// <summary>
/// The dead man's switch transport.
/// </summary>
public interface IHeartbeat
{
    /// <summary>
    /// Tells the check the job it watches has just succeeded.
    /// </summary>
    Task Ping(HeartbeatCheck check, CancellationToken ct = default);
}
