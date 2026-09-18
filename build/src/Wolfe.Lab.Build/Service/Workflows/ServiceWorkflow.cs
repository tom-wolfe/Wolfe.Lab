using Wolfe.Lab.Build.Backup.Jobs;
using Wolfe.Lab.Build.Service.Models;

namespace Wolfe.Lab.Build.Service.Workflows;

/// <summary>
/// A compose slice the shell still deploys: <c>"workflow": "service"</c>. The CLI's job in it
/// is the backup; deploy joins it when <c>scripts/deploy.sh</c> moves here.
/// </summary>
public sealed class ServiceWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "service";

    /// <inheritdoc />
    public string Label => "service";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new BackupJob<ServiceSettings>()];
}
