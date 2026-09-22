namespace Wolfe.Lab.Build.Workflows.Immich.Models;

/// <summary>
/// Where the Takeout is said to be, before anyone has looked.
/// </summary>
/// <param name="Directory">The directory that should hold the parts.</param>
public sealed record TakeoutLocation(IDirectory Directory);
