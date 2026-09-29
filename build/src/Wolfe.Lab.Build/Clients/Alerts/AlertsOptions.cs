namespace Wolfe.Lab.Build.Clients.Alerts;

/// <summary>
/// Where alerts go, from <c>Alerts</c> in <c>appsettings.json</c>.
/// </summary>
public sealed class AlertsOptions
{
    /// <summary>
    /// Pushover's messages endpoint.
    /// </summary>
    public Uri? Endpoint { get; set; }
}
