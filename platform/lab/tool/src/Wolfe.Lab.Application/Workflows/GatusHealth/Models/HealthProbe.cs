using Wolfe.Lab.Domain.Services;

namespace Wolfe.Lab.Application.Workflows.GatusHealth.Models;

/// <summary>
/// What the probe reads.
/// </summary>
/// <param name="Url">The health endpoint.</param>
public sealed record HealthProbe(ServiceUrl Url);
