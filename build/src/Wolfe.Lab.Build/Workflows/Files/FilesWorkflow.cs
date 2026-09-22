using Wolfe.Lab.Build.Backup;
using Wolfe.Lab.Build.Workflows.Files.Models;

namespace Wolfe.Lab.Build.Workflows.Files;

/// <summary>
/// The personal files: <c>"workflow": "files"</c>. Nothing runs, so the only job is the backup.
/// </summary>
public sealed class FilesWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "files";

    /// <inheritdoc />
    public string Label => "files";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new BackupJob<FilesSettings>(), new RestoreJob<FilesSettings>(), new DrillJob<FilesSettings>()];
}
