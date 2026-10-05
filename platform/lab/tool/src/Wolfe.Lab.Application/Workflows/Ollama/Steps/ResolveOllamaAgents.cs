using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Infrastructure.Agents;

namespace Wolfe.Lab.Application.Workflows.Ollama.Steps;

/// <summary>
/// The model server's agent, as the component's <c>ritten.json</c> declares it: Ollama's agents
/// stay there until its nodes' differences — models and roles, not just placement — have a home
/// in the catalog.
/// </summary>
[Step("resolve ollama agents", StepKind.Work)]
internal sealed class ResolveOllamaAgents(OllamaAgents declared)
{
    public StepResult<AgentDeclarations> Run() => new AgentDeclarations(declared.Agents);
}
