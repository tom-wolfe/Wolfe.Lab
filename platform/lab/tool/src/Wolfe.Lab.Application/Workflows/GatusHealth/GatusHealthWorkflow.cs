using Wolfe.Lab.Application.Workflows.GatusHealth.Jobs;

namespace Wolfe.Lab.Application.Workflows.GatusHealth;

/// <summary>
/// Gatus, watched from the mini: <c>"workflow": "gatus-health"</c>.
/// </summary>
/// <remarks>
/// A dead status page looks like one you haven't opened, so the mini asks it on a schedule.
/// </remarks>
public sealed class GatusHealthWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "gatus-health";

    /// <inheritdoc />
    public override string Label => "gatus health";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new ProbeJob()];
}
