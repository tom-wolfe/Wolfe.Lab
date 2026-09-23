using Ritten.Docker;

namespace Wolfe.Lab.Build.Workflows.Docker.Models;

/// <summary>
/// The images an image component declares, as its check judges them.
/// </summary>
/// <param name="Images">The images.</param>
/// <param name="Pushed">Whether the component names a registry, so every tag must name its host.</param>
public sealed record ComponentImages(IReadOnlyList<DockerImage> Images, bool Pushed);
