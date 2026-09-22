using Wolfe.Lab.Build.Clients.Restic;

namespace Wolfe.Lab.Build.Backup;

/// <summary>
/// The snapshot a restore brings back.
/// </summary>
/// <param name="Snapshot">The snapshot chosen.</param>
public sealed record RestorePoint(ResticSnapshot Snapshot);
