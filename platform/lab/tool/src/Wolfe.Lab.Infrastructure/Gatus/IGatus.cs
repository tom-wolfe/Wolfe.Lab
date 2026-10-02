using Wolfe.Lab.Domain.Services;

namespace Wolfe.Lab.Infrastructure.Gatus;

/// <summary>
/// A Gatus instance, asked the one thing it can say about itself.
/// </summary>
public interface IGatus
{
    /// <summary>
    /// Reads the instance's own health endpoint.
    /// </summary>
    /// <param name="url">The <c>/health</c> URL.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task<GatusStatus> Health(ServiceUrl url, CancellationToken ct = default);
}
