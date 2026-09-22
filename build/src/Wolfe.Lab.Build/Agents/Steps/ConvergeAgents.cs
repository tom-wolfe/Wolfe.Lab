namespace Wolfe.Lab.Build.Agents.Steps;

/// <summary>
/// Makes the node's supervisor match the declaration, one agent at a time.
/// </summary>
[Step("converge agents", StepKind.Publish)]
internal sealed class ConvergeAgents(IServiceSupervisor supervisor, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(AgentPlan plan, CancellationToken ct = default)
    {
        foreach (var agent in plan.Agents)
        {
            var outcome = await supervisor.Converge(agent, ct);
            if (job.DryRun)
            {
                // The rehearsal has already said what it would do, in the conditional. Saying
                // it again here would be the job claiming it happened.
                continue;
            }

            switch (outcome)
            {
                case AgentOutcome.Unchanged:
                    log.Detail($"{agent.Label.Value} is current.");
                    break;
                case AgentOutcome.Installed:
                    log.Status($"Installed and started {agent.Label.Value}.");
                    break;
                case AgentOutcome.Restarted:
                    log.Status($"Restarted {agent.Label.Value} on a changed unit.");
                    break;
            }
        }

        return StepResult.Successful;
    }
}
