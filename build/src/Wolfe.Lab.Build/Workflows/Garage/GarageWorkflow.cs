using Wolfe.Lab.Build.Backup;
using Wolfe.Lab.Build.Deploy;
using Wolfe.Lab.Build.Workflows.Garage.Jobs;
using Wolfe.Lab.Build.Workflows.Garage.Models;

namespace Wolfe.Lab.Build.Workflows.Garage;

/// <summary>
/// Object storage: an ordinary compose slice with its backups, plus the one-time cluster layout
/// that has to exist before the admin API is any use.
/// </summary>
public sealed class GarageWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "garage";

    /// <inheritdoc />
    public string Label => "garage";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } =
    [
        new DeployJob<GarageSettings>(),
        new BackupJob<GarageSettings>(),
        new RestoreJob<GarageSettings>(),
        new DrillJob<GarageSettings>(),
        new InitLayoutJob()
    ];
}
