using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Alerts;

/// <summary>
/// Pushover, the way <c>scripts/alert.sh</c> sends it: low priority, so a failed job is a
/// notification rather than an interruption, with the credentials read from the vault at the
/// moment of sending.
/// </summary>
internal sealed class PushoverAlerts(HttpClient http, ISecrets secrets) : IAlerts
{
    internal static readonly Uri Endpoint = new("https://api.pushover.net/1/messages.json");
    internal static readonly SecretReference Token = SecretReference.From("op://Wolfe.Lab/pushover/credential");
    internal static readonly SecretReference User = SecretReference.From("op://Wolfe.Lab/pushover/username");
    private const string Priority = "-1";

    /// <inheritdoc />
    public async Task Send(Alert alert, CancellationToken ct = default)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = await secrets.Read(Token, ct),
            ["user"] = await secrets.Read(User, ct),
            ["title"] = alert.Title,
            ["message"] = alert.Message,
            ["priority"] = Priority
        });
        using var response = await http.PostAsync(Endpoint, form, ct);
        response.EnsureSuccessStatusCode();
    }
}
