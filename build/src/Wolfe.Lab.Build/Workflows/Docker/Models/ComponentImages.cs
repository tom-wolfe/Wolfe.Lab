namespace Wolfe.Lab.Build.Workflows.Docker.Models;

/// <summary>
/// The images an image component declares, as its check judges them.
/// </summary>
/// <param name="Images">The images.</param>
public sealed record ComponentImages(IReadOnlyList<ComponentImage> Images);
