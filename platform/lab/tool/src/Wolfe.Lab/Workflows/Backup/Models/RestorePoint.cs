using Wolfe.Lab.Clients.Restic;

namespace Wolfe.Lab.Workflows.Backup.Models;

/// <summary>
/// The snapshot a restore brings back.
/// </summary>
/// <param name="Snapshot">The snapshot chosen.</param>
public sealed record RestorePoint(ResticSnapshot Snapshot);
