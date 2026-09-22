namespace Wolfe.Lab.Build.Workflows.Chezmoi.Models;

/// <summary>
/// Every profile's rendered tree, for the checks that read the output rather than the source.
/// </summary>
/// <param name="Profiles">The renders, in declaration order.</param>
public sealed record RenderedProfiles(IReadOnlyList<RenderedProfile> Profiles);
