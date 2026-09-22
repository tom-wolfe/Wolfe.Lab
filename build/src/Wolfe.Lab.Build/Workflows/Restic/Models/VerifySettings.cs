namespace Wolfe.Lab.Build.Workflows.Restic.Models;

/// <summary>
/// How the weekly check samples the offsite copy.
/// </summary>
public sealed record VerifySettings
{
    /// <summary>
    /// The share of pack data read back from the offsite repository, as restic spells it
    /// (<c>5%</c>). Over a year a small sample covers most of the repository, for pennies.
    /// </summary>
    public string ReadDataSubset { get; init; } = "5%";
}
