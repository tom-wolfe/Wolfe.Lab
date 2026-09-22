namespace Wolfe.Lab.Build.Clients.Heartbeat;

/// <summary>
/// healthchecks.io: one GET by slug, the ping key
/// read from the vault at the moment of use.
/// </summary>
internal sealed class HealthchecksHeartbeat(HttpClient http, ISecretProvider secrets) : IHeartbeat
{
    private static readonly Uri Endpoint = new("https://hc-ping.com/");

    /// <inheritdoc />
    public async Task Ping(HeartbeatCheck check, CancellationToken ct = default)
    {
        var key = await secrets.Resolve(check.Key.Value, ct);
        using var response = await http.GetAsync(new Uri(Endpoint, $"{key}/{check.Slug}"), ct);
        response.EnsureSuccessStatusCode();
    }
}
