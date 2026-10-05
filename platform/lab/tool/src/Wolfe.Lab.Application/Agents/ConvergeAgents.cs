using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Agents;

/// <summary>
/// Makes the node's supervisor match the declaration, one agent at a time.
/// </summary>
[Step("converge agents", StepKind.Publish)]
internal sealed class ConvergeAgents(IServiceSupervisor supervisor, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(AgentPlan plan, PublishedArtifacts artifacts, CancellationToken ct = default)
    {
        // The artifacts' stamp goes into every unit, so a changed config file restarts the agents
        // that read it, the way a changed binary does.
        foreach (var agent in plan.Agents.Select(agent => agent with { ArtifactStamp = artifacts.Stamp }))
        {
            // Before the converge, so the old copy has stopped by the time the new one starts:
            // two agents with one identity would each think they were the node.
            foreach (var unit in agent.Supersedes)
            {
                await supervisor.Retire(unit, ct);
            }

            // Neither launchd nor systemd makes the directory a log is written to, and an agent
            // whose log cannot be opened never starts.
            if (!job.DryRun && agent.Log is { } output)
            {
                output.File.Directory.Create();
            }

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
                    log.Status($"Restarted {agent.Label.Value} on a changed unit or changed artifacts.");
                    break;
            }
        }

        return StepResult.Successful;
    }
}
