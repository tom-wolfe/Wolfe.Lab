
using Wolfe.Lab.Application.Workflows.Docker.Jobs;

namespace Wolfe.Lab.Application.Workflows.Docker;

/// <summary>
/// Workflows for docker images.
/// </summary>
/// <remarks>
/// The docker workflow's smaller sibling, for a component that ships an image and no stack.
/// </remarks>
public sealed class ImageWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "image";

    /// <inheritdoc />
    public string Label => "image";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new ImageCheckJob()];
}
