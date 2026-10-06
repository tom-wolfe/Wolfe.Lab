using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Infrastructure.Agents;

namespace Wolfe.Lab.Application.Agents;

/// <summary>
/// What this node runs of the component: its agent as the catalog declares it, expanded for the node.
/// </summary>
/// <remarks>
/// The node is where the deploy runs (<see cref="AgentResolver.ThisNode"/>), never an argument to
/// it. A component placed elsewhere is nothing to do here, and the deploy stops.
/// </remarks>
[Step("resolve agent declarations", StepKind.Work)]
internal sealed class ResolveAgentDeclarations(AgentResolver agents, WorkflowJob job, IWorkflowLog log)
{
    public StepResult<AgentDeclarations> Run(ServiceCatalog catalog, DeploymentUnit unit)
    {
        if (!unit.ByWorkflow(job.WorkflowName).TryGetValue(out var component, out var errors))
        {
            return StepResult.Failed(errors);
        }

        if (component is not AgentComponent agent)
        {
            return AgentDeclarationErrors.NoAgent(component);
        }

        if (!agents.ThisNode(catalog).TryGetValue(out var node, out var nowhere))
        {
            return StepResult.Failed(nowhere);
        }

        if (!agent.RunsOn.Includes(node))
        {
            log.Status($"{agent} runs on {agent.RunsOn}, which is not {node}: nothing to do here.");
            return StepResult.NothingToDo;
        }

        if (!agents.On(agent, node).TryGetValue(out var declarations, out var invalid))
        {
            return StepResult.Failed(invalid);
        }

        log.Detail($"{agent} runs on {node} as {string.Join(", ", declarations.Agents.Keys)}.");
        return declarations;
    }
}
