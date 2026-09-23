using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Clients.Gatus;

/// <summary>
/// Gatus over HTTP: one GET, one JSON field.
/// </summary>
internal sealed class GatusClient(HttpClient http) : IGatus
{
    /// <inheritdoc />
    public async Task<GatusStatus> Health(ServiceUrl url, CancellationToken ct = default)
    {
        using var response = await http.GetAsync(new Uri(url.Value), ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<HealthBody>(ct);
        return new GatusStatus(body?.Status ?? "");
    }

    private sealed record HealthBody([property: JsonPropertyName("status")] string? Status);
}
