using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Heartbeat;

/// <summary>
/// healthchecks.io, the way <c>scripts/heartbeat.sh</c> pings it: one GET by slug, the ping key
/// read from the vault at the moment of use.
/// </summary>
internal sealed class HealthchecksHeartbeat(HttpClient http, ISecrets secrets) : IHeartbeat
{
    private static readonly Uri Endpoint = new("https://hc-ping.com/");

    /// <inheritdoc />
    public async Task Ping(HeartbeatCheck check, CancellationToken ct = default)
    {
        var key = await secrets.Read(check.Key, ct);
        using var response = await http.GetAsync(new Uri(Endpoint, $"{key}/{check.Slug}"), ct);
        response.EnsureSuccessStatusCode();
    }
}
