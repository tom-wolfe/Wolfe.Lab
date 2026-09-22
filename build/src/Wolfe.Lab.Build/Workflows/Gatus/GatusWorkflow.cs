using Wolfe.Lab.Build.Deploy;
using Wolfe.Lab.Build.Workflows.Gatus.Jobs;
using Wolfe.Lab.Build.Workflows.Gatus.Models;

namespace Wolfe.Lab.Build.Workflows.Gatus;

/// <summary>
/// The status page: an ordinary compose deploy, plus the probe that watches the watcher.
/// </summary>
/// <remarks>
/// Gatus's own alerts cannot report Gatus being down, and its deploy is a convergent no-op that
/// stays green regardless, so without the probe a dead status page looks exactly like one nobody
/// has opened. The probe runs from the other machine for the same reason the heartbeat runs
/// from outside the lab: a watcher must not share the fate of what it watches.
/// </remarks>
public sealed class GatusWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "gatus";

    /// <inheritdoc />
    public string Label => "gatus";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new DeployJob<GatusSettings>(), new HealthJob()];
}
