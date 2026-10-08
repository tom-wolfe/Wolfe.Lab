using Wolfe.Lab.Application.Workflows.Backup.Jobs;

namespace Wolfe.Lab.Application.Workflows.Backup;

/// <summary>
/// A component's state in restic: <c>"workflow": "backup"</c>.
/// </summary>
/// <remarks>
/// One per service with state worth keeping, beside its Docker component: what a snapshot holds,
/// what has to be quiet while it is taken, and what a restore must bring back. The restic service
/// owns the repositories; this component owns what goes into them.
/// </remarks>
public sealed class BackupWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "backup";

    /// <inheritdoc />
    public override string Label => "backup";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new BackupJob(), new RestoreJob(), new DrillJob()];
}
