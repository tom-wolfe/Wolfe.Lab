using Wolfe.Lab.Build.Workflows.Heartbeat.Jobs;

namespace Wolfe.Lab.Build.Workflows.Heartbeat;

/// <summary>
/// The dead man's switch: the one signal that can report "the lab is off" comes from a ping the
/// lab sends out, and stops sending when it dies.
/// </summary>
public sealed class HeartbeatWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "heartbeat";

    /// <inheritdoc />
    public string Label => "heartbeat";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new PingJob()];
}
