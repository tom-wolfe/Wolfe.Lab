namespace Wolfe.Lab.Build.Backup.Models;

/// <summary>
/// A snapshot restic saved.
/// </summary>
/// <param name="Id">restic's short id for it.</param>
public sealed record Snapshot(string Id);
