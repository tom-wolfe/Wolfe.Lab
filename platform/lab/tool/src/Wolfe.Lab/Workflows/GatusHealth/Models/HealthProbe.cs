using Wolfe.Lab.Values;

namespace Wolfe.Lab.Workflows.GatusHealth.Models;

/// <summary>
/// What the probe reads.
/// </summary>
/// <param name="Url">The health endpoint.</param>
public sealed record HealthProbe(ServiceUrl Url);
