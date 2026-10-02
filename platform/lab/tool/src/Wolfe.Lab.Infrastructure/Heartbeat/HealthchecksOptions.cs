namespace Wolfe.Lab.Infrastructure.Heartbeat;

/// <summary>
/// Where the heartbeat pings, from <c>Heartbeat</c> in <c>appsettings.json</c>.
/// </summary>
public sealed class HealthchecksOptions
{
    /// <summary>
    /// healthchecks.io's ping base: a check is pinged at <c>&lt;base&gt;&lt;key&gt;/&lt;slug&gt;</c>.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// The endpoint, which registration validated is set.
    /// </summary>
    internal Uri RequiredEndpoint => Endpoint ?? throw new InvalidOperationException("'Heartbeat:Endpoint' is not set in appsettings.json.");
}
