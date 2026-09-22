
using Wolfe.Lab.Build.Workflows.Docker.Jobs;

namespace Wolfe.Lab.Build.Workflows.Docker;

/// <summary>
/// Workflows for docker images.
/// </summary>
/// <remarks>
/// The docker workflow's smaller sibling, for a component that ships an image and no stack —
/// the CI image being the one that matters, since every containerised job runs in it.
/// </remarks>
public sealed class ImageWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "image";

    /// <inheritdoc />
    public string Label => "image";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new BuildJob()];
}
