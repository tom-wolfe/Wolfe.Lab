namespace Wolfe.Lab.Clients.Resilience;

/// <summary>
/// One wait's options, from its section of <c>appsettings.json</c>.
/// </summary>
public sealed class PollingOptions
{
    /// <summary>
    /// When the wait gives up.
    /// </summary>
    public TimeSpan Limit { get; set; }

    /// <summary>
    /// The pause before asking again.
    /// </summary>
    public TimeSpan Interval { get; set; }
}
