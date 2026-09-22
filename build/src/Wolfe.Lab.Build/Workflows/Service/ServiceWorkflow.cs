using Wolfe.Lab.Build.Backup;
using Wolfe.Lab.Build.Deploy;
using Wolfe.Lab.Build.Workflows.Service.Models;

namespace Wolfe.Lab.Build.Workflows.Service;

/// <summary>
/// A compose slice.
/// </summary>
public sealed class ServiceWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "service";

    /// <inheritdoc />
    public string Label => "service";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new DeployJob<ServiceSettings>(), new BackupJob<ServiceSettings>(), new RestoreJob<ServiceSettings>(), new DrillJob<ServiceSettings>()];
}
