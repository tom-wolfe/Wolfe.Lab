namespace Wolfe.Lab.Build.Immich.Models;

/// <summary>
/// How the import runs.
/// </summary>
/// <param name="Concurrency">Parallel uploads.</param>
public sealed record ImportOptions(int Concurrency);
