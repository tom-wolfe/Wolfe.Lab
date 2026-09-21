using Wolfe.Lab.Build.Docker.Jobs;
using Wolfe.Lab.Build.Docker.Models;

namespace Wolfe.Lab.Build.Docker.Workflows;

/// <summary>
/// A compose project: <c>"workflow": "docker"</c>.
/// </summary>
/// <remarks>
/// Written once, for a shape that repeats. Every slice that runs containers has one of these
/// and they differ only in what they declare — which is the point of splitting components out
/// of slices: the regular part gets one workflow, and the slice keeps only what is genuinely
/// its own.
/// </remarks>
public sealed class DockerWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "docker";

    /// <inheritdoc />
    public string Label => "docker";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new CheckJob<DockerSettings>(), new DeployJob<DockerSettings>()];
}
