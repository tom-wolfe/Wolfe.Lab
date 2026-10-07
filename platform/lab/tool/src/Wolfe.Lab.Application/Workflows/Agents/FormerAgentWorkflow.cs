namespace Wolfe.Lab.Application.Workflows.Agents;

/// <summary>
/// The agent workflow under the name it went by once, <c>agents</c>, for a <c>ritten.json</c> that
/// still names it so: the same jobs, until none does.
/// </summary>
public sealed class FormerAgentWorkflow : IWorkflow
{
    private readonly AgentWorkflow _workflow = new();

    /// <inheritdoc />
    public string Name => "agents";

    /// <inheritdoc />
    public string Label => _workflow.Label;

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs => _workflow.Jobs;
}
