namespace Wolfe.Lab.Build.Slices;

/// <summary>
/// The external volumes a deploy must find mounted.
/// </summary>
/// <param name="Directories">The volumes' mount points.</param>
public sealed record RequiredVolumes(IReadOnlyList<IDirectory> Directories);
