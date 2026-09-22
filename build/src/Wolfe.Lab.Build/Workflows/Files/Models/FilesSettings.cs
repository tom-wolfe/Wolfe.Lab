using Wolfe.Lab.Build.Backup;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Workflows.Files.Models;

/// <summary>
/// The shape of <c>files/ritten.json</c>: a slice that is data, so nothing beyond what every
/// slice declares.
/// </summary>
public sealed record FilesSettings : SliceSettings, IBackupSettings
{
    /// <inheritdoc />
    public BackupSettings Backup { get; init; } = new();
}
