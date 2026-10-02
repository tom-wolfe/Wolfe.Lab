using Wolfe.Lab.Workflows.Backup.Jobs;

namespace Wolfe.Lab.Workflows.Backup;

/// <summary>
/// A component's state in restic: <c>"workflow": "backup"</c>.
/// </summary>
/// <remarks>
/// One per service with state worth keeping, beside its compose component: what a snapshot holds,
/// what has to be quiet while it is taken, and what a restore must bring back. The restic service
/// owns the repositories; this component owns what goes into them.
/// </remarks>
public sealed class BackupWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "backup";

    /// <inheritdoc />
    public string Label => "backup";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new BackupJob(), new RestoreJob(), new DrillJob()];
}
