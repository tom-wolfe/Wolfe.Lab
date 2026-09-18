namespace Wolfe.Lab.Build.Immich.Models;

/// <summary>
/// A Google Takeout on the node: the directory its zip parts sit in, and the parts by name.
/// immich-go reads a directory only as an extracted Takeout; archives are named one by one.
/// </summary>
/// <param name="Directory">The directory holding the parts.</param>
/// <param name="Parts">The zip parts' file names, in order.</param>
public sealed record Takeout(IDirectory Directory, IReadOnlyList<string> Parts);
