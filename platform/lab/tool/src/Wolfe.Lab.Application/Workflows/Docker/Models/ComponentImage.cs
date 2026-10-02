namespace Wolfe.Lab.Application.Workflows.Docker.Models;

/// <summary>
/// One image an image component builds and pushes.
/// </summary>
/// <param name="Tag">The tag it is pushed under, which must name its registry.</param>
/// <param name="Context">The build context, relative to the component.</param>
/// <param name="Dockerfile">The Dockerfile, relative to the context.</param>
public sealed record ComponentImage(string Tag, string Context, string Dockerfile);
