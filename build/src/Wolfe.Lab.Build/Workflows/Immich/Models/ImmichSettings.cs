using Wolfe.Lab.Build.Backup;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Workflows.Immich.Models;

/// <summary>
/// The shape of <c>immich/ritten.json</c>.
/// </summary>
public sealed record ImmichSettings : SliceSettings, IBackupSettings
{
    /// <inheritdoc />
    public BackupSettings Backup { get; init; } = new();

    /// <summary>
    /// The Immich server as the import reaches it.
    /// </summary>
    public ServerSettings Server { get; init; } = new();

    /// <summary>
    /// The Google Takeout to import.
    /// </summary>
    public TakeoutSettings Takeout { get; init; } = new();

    /// <summary>
    /// How the import runs.
    /// </summary>
    public ImportSettings Import { get; init; } = new();
}
