using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Gatus.Models;

/// <summary>
/// The <c>health</c> section: the instance's <c>/health</c> URL, as reached from the machine the
/// probe runs on.
/// </summary>
public sealed record HealthSettings
{
    /// <summary>
    /// The health endpoint.
    /// </summary>
    public ServiceUrl? Url { get; init; }

    /// <summary>
    /// The probe the step makes, or null while the URL is missing.
    /// </summary>
    public HealthProbe? ToProbe() => Url is { } url ? new HealthProbe(url) : null;
}
