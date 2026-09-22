using Wolfe.Lab.Build.Backup;
using Wolfe.Lab.Build.Deploy;
using Wolfe.Lab.Build.Workflows.Immich.Jobs;
using Wolfe.Lab.Build.Workflows.Immich.Models;

namespace Wolfe.Lab.Build.Workflows.Immich;

/// <summary>
/// The photo library: <c>"workflow": "immich"</c>.
/// </summary>
public sealed class ImmichWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "immich";

    /// <inheritdoc />
    public string Label => "immich";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [
        new DeployJob<ImmichSettings>(),
        new ImportJob(),
        new BackupJob<ImmichSettings>(),
        new RestoreJob<ImmichSettings>(),
        new DrillJob<ImmichSettings>()
    ];
}
