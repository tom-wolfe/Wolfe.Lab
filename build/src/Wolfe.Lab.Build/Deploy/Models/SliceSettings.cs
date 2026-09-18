using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Paths;

namespace Wolfe.Lab.Build.Deploy.Models;

/// <summary>
/// What every deployable slice's <c>ritten.json</c> shares.
/// </summary>
public record SliceSettings : WorkflowSettings
{
    /// <summary>
    /// External volumes the stack binds.
    /// </summary>
    public IReadOnlyList<HostPath> Volumes { get; init; } = [];

    /// <summary>
    /// What the slice's backup snapshots, for a workflow that offers one.
    /// </summary>
    public BackupSettings? Backup { get; init; }
}
