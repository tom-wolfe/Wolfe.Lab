namespace Wolfe.Lab.Build.Workflows.Docker.Models;

/// <summary>
/// The images a deploy will build, in the order declared.
/// </summary>
/// <param name="Images">The images.</param>
public sealed record ImagePlan(IReadOnlyList<BuildableImage> Images);
