using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.GatusHealth.Models;

/// <summary>
/// The shape of the health component's <c>ritten.json</c>: <c>"workflow": "gatus-health"</c>.
/// </summary>
public sealed record GatusHealthSettings : WorkflowSettings
{
    /// <summary>
    /// Gatus's own health endpoint.
    /// </summary>
    public ServiceUrl? Url { get; init; }

    /// <summary>
    /// The probe as the step runs it, or null while the url is missing.
    /// </summary>
    public HealthProbe? ToProbe() => Url is { } url ? new HealthProbe(url) : null;
}
