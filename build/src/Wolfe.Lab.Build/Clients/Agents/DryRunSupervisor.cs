
namespace Wolfe.Lab.Build.Clients.Agents;

/// <summary>
/// Says what converging would do.
/// </summary>
internal sealed class DryRunSupervisor(IWorkflowLog log, PlatformSupervisor platform) : IServiceSupervisor
{
    private readonly IServiceSupervisor _inner = platform.Supervisor;

    /// <inheritdoc />
    public AgentUnit Render(AgentDefinition agent) => _inner.Render(agent);

    /// <inheritdoc />
    public Task<AgentOutcome> Plan(AgentDefinition agent, CancellationToken ct = default) => _inner.Plan(agent, ct);

    /// <inheritdoc />
    public async Task<AgentOutcome> Converge(AgentDefinition agent, CancellationToken ct = default)
    {
        var outcome = await _inner.Plan(agent, ct);
        var what = outcome switch
        {
            AgentOutcome.Unchanged => $"{agent.Label.Value} is already what the slice declares.",
            AgentOutcome.Installed => $"Would install and start {agent.Label.Value}.",
            AgentOutcome.Restarted => $"Would rewrite {agent.Label.Value}'s unit and restart it.",
            _ => $"Would converge {agent.Label.Value}."
        };

        log.Skipped(what);
        return outcome;
    }
}
