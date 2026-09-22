using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Slices;

/// <summary>
/// What every deployable slice's <c>ritten.json</c> shares.
/// </summary>
public record SliceSettings : WorkflowSettings
{
    /// <summary>
    /// External volumes the stack binds.
    /// </summary>
    public IReadOnlyList<HostPath> Volumes { get; init; } = [];
}
