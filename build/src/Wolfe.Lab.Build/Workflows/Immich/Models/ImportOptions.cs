namespace Wolfe.Lab.Build.Workflows.Immich.Models;

/// <summary>
/// How the import runs.
/// </summary>
/// <param name="Concurrency">Parallel uploads.</param>
public sealed record ImportOptions(int Concurrency);
