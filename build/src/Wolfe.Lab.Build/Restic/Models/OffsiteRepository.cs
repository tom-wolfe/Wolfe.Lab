namespace Wolfe.Lab.Build.Restic.Models;

/// <summary>
/// The offsite repository, whose environment also names the local one as the copy's source, so
/// <c>restic copy</c> needs no flags.
/// </summary>
/// <param name="Repository">The repository on B2, as restic is told about it.</param>
public sealed record OffsiteRepository(ResticRepository Repository);
