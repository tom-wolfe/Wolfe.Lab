using Wolfe.Lab.Build.Backup;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Workflows.Service.Models;

/// <summary>
/// The shape of a compose slice's <c>ritten.json</c> when the CLI's part in it is the backup:
/// nothing beyond what every slice declares.
/// </summary>
public sealed record ServiceSettings : SliceSettings, IBackupSettings
{
    /// <inheritdoc />
    public BackupSettings Backup { get; init; } = new();
}
