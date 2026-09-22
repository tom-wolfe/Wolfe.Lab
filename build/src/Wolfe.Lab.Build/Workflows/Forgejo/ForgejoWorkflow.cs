using Wolfe.Lab.Build.Backup;
using Wolfe.Lab.Build.Deploy;
using Wolfe.Lab.Build.Workflows.Forgejo.Jobs;
using Wolfe.Lab.Build.Workflows.Forgejo.Models;

namespace Wolfe.Lab.Build.Workflows.Forgejo;

/// <summary>
/// The forge: an ordinary compose slice with its backups, plus the one thing only it can do —
/// tell itself about a node's Actions runner.
/// </summary>
public sealed class ForgejoWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "forgejo";

    /// <inheritdoc />
    public string Label => "forgejo";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } =
    [
        new DeployJob<ForgejoSettings>(),
        new BackupJob<ForgejoSettings>(),
        new RestoreJob<ForgejoSettings>(),
        new DrillJob<ForgejoSettings>(),
        new RegisterRunnerJob()
    ];
}
