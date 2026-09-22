namespace Wolfe.Lab.Build.Workflows.Chezmoi.Models;

/// <summary>
/// One profile's rendered tree.
/// </summary>
/// <param name="Profile">The profile.</param>
/// <param name="Directory">Where its tree was rendered.</param>
/// <param name="Files">The files in it, relative and sorted.</param>
public sealed record RenderedProfile(string Profile, IDirectory Directory, IReadOnlyList<string> Files);
