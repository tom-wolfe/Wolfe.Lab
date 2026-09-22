namespace Wolfe.Lab.Build.Clients.Alerts;

/// <summary>
/// The lab's alert transport.
/// </summary>
public interface IAlerts
{
    /// <summary>
    /// Sends the alert.
    /// </summary>
    Task Send(Alert alert, CancellationToken ct = default);
}
