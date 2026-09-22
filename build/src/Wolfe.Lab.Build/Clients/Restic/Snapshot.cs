namespace Wolfe.Lab.Build.Clients.Restic;

/// <summary>
/// A snapshot restic saved.
/// </summary>
/// <param name="Id">restic's short id for it.</param>
public sealed record Snapshot(string Id);
