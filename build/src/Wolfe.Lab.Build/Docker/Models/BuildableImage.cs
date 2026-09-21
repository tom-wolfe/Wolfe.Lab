namespace Wolfe.Lab.Build.Docker.Models;

/// <summary>
/// An image the deploy will build.
/// </summary>
/// <param name="Tag">The tag to give it.</param>
/// <param name="Context">The build context, relative to the component.</param>
public sealed record BuildableImage(string Tag, string Context);
