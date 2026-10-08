using Microsoft.Extensions.Options;
using Wolfe.Lab.Domain.Catalog.Facets.Heartbeats;

namespace Wolfe.Lab.Infrastructure.Heartbeat;

/// <summary>
/// healthchecks.io: one GET by slug, the ping key
/// read from the vault at the moment of use.
/// </summary>
internal sealed class HealthchecksHeartbeat(HttpClient http, ISecretProvider secrets, IOptions<HealthchecksOptions> options) : IHeartbeat
{
    /// <inheritdoc />
    public async Task Ping(HeartbeatCheck check, CancellationToken ct = default)
    {
        var key = await secrets.Resolve(check.Key.Value, ct);
        using var response = await http.GetAsync(new Uri(options.Value.RequiredEndpoint, $"{key}/{check.Slug}"), ct);
        response.EnsureSuccessStatusCode();
    }
}
