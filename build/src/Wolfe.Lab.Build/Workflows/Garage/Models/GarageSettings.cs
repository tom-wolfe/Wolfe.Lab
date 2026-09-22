using Wolfe.Lab.Build.Backup;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Workflows.Garage.Models;

/// <summary>
/// Garage's <c>ritten.json</c>: what every slice declares, its backup, and the cluster layout.
/// </summary>
public sealed record GarageSettings : SliceSettings, IBackupSettings
{
    /// <inheritdoc />
    public BackupSettings Backup { get; init; } = new();

    /// <summary>
    /// The one node's role in the cluster.
    /// </summary>
    public LayoutSettings Layout { get; init; } = new();
}
