using Wolfe.Lab.Build.Backup;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Workflows.Forgejo.Models;

/// <summary>
/// The forge's <c>ritten.json</c>: what every slice declares, its backup, and how runners register.
/// </summary>
public sealed record ForgejoSettings : SliceSettings, IBackupSettings
{
    /// <inheritdoc />
    public BackupSettings Backup { get; init; } = new();

    /// <summary>
    /// What every runner registration shares.
    /// </summary>
    public RunnerSettings Runners { get; init; } = new();
}
