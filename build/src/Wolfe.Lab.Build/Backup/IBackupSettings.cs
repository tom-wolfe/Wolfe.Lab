
namespace Wolfe.Lab.Build.Backup;

/// <summary>
/// The settings of a slice that offers the backup jobs: the ones that carry a <c>backup</c> section.
/// </summary>
/// <remarks>
/// A workflow lists the backup jobs beside its own, and its settings record says so by carrying
/// the section those jobs read. The section is declared where it is offered rather than on every
/// slice, so a slice that is never snapshotted has no <c>backup</c> key to leave empty.
/// </remarks>
public interface IBackupSettings
{
    /// <summary>
    /// What a snapshot holds, and what has to be quiet while it is taken.
    /// </summary>
    BackupSettings Backup { get; }
}
