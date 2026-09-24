using Wolfe.Lab.Build.Workflows.Agents.Jobs;

namespace Wolfe.Lab.Build.Workflows.Agents;

/// <summary>
/// Host processes a slice runs on its nodes.
/// </summary>
/// <remarks>
/// The node services that used to live in chezmoi — a monitoring agent, a runner — as a component
/// like any other: declared per node, checked on the pull request, and deployed to each node by
/// its own runner, launchd on a Mac and systemd on Linux.
/// </remarks>
public sealed class AgentsWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "agents";

    /// <inheritdoc />
    public string Label => "agents";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new CheckJob(), new DeployJob()];
}
