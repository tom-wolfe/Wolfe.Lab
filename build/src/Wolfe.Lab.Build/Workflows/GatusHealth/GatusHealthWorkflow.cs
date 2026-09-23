using Wolfe.Lab.Build.Workflows.GatusHealth.Jobs;

namespace Wolfe.Lab.Build.Workflows.GatusHealth;

/// <summary>
/// Gatus, watched from the mini: <c>"workflow": "gatus-health"</c>.
/// </summary>
/// <remarks>
/// A dead status page looks like one you haven't opened, so the mini asks it on a schedule.
/// </remarks>
public sealed class GatusHealthWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "gatus-health";

    /// <inheritdoc />
    public string Label => "gatus health";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new ProbeJob()];
}
