using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Infrastructure.Agents;

namespace Wolfe.Lab.Application.Workflows.Ollama.Steps;

/// <summary>
/// Works out what this node's server runs, for its pinned release to be installed and put first on
/// the job's path: what the job runs of the server's own — <c>ollama pull</c>, <c>ollama create</c> —
/// is then the version the server runs, on a node that has no other.
/// </summary>
[Step("resolve server agent", StepKind.Work)]
internal sealed class ResolveServerAgent(AgentResolver agents, IWorkflowLog log)
{
    public StepResult<AgentDeclarations> Run(ServiceCatalog catalog, DeploymentUnit unit, ServerPlan plan)
    {
        if (plan.Server is not { } name || unit.Service.FindComponent(name) is not AgentComponent server)
        {
            return new AgentDeclarations(new Dictionary<string, AgentOptions>());
        }

        if (!agents.ThisNode(catalog).TryGetValue(out var node, out var elsewhere))
        {
            return StepResult.Failed(elsewhere);
        }

        if (!agents.On(server, node).TryGetValue(out var declarations, out var unresolved))
        {
            return StepResult.Failed(unresolved);
        }

        log.Detail($"{server.Name} runs {string.Join(", ", declarations.Agents.Keys)} on {node.Name}.");
        return declarations;
    }
}
