
using Wolfe.Lab.Application.Workflows.Docker.Jobs;
using Wolfe.Lab.Application.Workflows.Docker.Models;

namespace Wolfe.Lab.Application.Workflows.Docker;

/// <summary>
/// A compose project: <c>"workflow": "docker"</c>.
/// </summary>
/// <remarks>
/// Written once, for a shape that repeats. Every service that runs containers has one of these
/// and they differ only in what they declare — which is the point of splitting a service into
/// components: the regular part gets one workflow, and what is genuinely the service's own gets a
/// component and a workflow of its own beside it.
/// </remarks>
public sealed class DockerWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "docker";

    /// <inheritdoc />
    public string Label => "docker";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new CheckJob<DockerComponentOptions>(), new DeployJob<DockerComponentOptions>()];
}
