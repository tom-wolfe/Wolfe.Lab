using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Workflows.Gatus.Models;

/// <summary>
/// The status page's <c>ritten.json</c>: what every slice declares, plus where its health is read.
/// </summary>
public sealed record GatusSettings : SliceSettings
{
    /// <summary>
    /// The health endpoint the probe reads.
    /// </summary>
    public HealthSettings Health { get; init; } = new();
}
