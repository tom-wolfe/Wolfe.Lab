namespace Wolfe.Lab.Build.Workflows.Immich.Models;

/// <summary>
/// How the import runs.
/// </summary>
public sealed record ImportSettings
{
    /// <summary>
    /// Parallel uploads. immich-go's own guidance for a large collection on a shared server.
    /// </summary>
    public int Concurrency { get; init; } = 4;
}
