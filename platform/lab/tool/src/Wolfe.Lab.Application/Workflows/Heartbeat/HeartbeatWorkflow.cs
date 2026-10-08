using Wolfe.Lab.Application.Workflows.Heartbeat.Jobs;

namespace Wolfe.Lab.Application.Workflows.Heartbeat;

/// <summary>
/// The dead man's switch: the one signal that can report "the lab is off" comes from a ping the
/// lab sends out, and stops sending when it dies.
/// </summary>
public sealed class HeartbeatWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "heartbeat";

    /// <inheritdoc />
    public override string Label => "heartbeat";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new PingJob()];
}
