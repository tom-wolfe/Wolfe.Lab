namespace Wolfe.Lab.Build.Slices;

/// <summary>
/// A slice being deployed.
/// </summary>
/// <param name="Name">The slice's directory name.</param>
/// <param name="Source">The slice in the checkout.</param>
/// <param name="Release">The installed copy compose runs from.</param>
public sealed record Slice(string Name, IDirectory Source, IDirectory Release);
