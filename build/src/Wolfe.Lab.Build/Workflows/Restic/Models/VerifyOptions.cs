namespace Wolfe.Lab.Build.Workflows.Restic.Models;

/// <summary>
/// How the check samples the offsite copy.
/// </summary>
/// <param name="ReadDataSubset">The share of pack data read back, as restic spells it.</param>
public sealed record VerifyOptions(string ReadDataSubset);
