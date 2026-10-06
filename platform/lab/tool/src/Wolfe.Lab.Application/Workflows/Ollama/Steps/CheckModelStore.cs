using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;

namespace Wolfe.Lab.Application.Workflows.Ollama.Steps;

/// <summary>
/// Holds the model store <c>ritten.json</c> names to the one the server is told, its
/// <c>OLLAMA_MODELS</c>.
/// </summary>
/// <remarks>
/// One directory named in two places is a pair that drifts, and the way it fails is the server
/// starting against an empty store and answering with no models at all.
/// </remarks>
[Step("check model store", StepKind.Check)]
internal sealed class CheckModelStore(ModelStore store, IWorkflowLog log)
{
    private const string AgentName = "ollama";
    private const string StoreVariable = "OLLAMA_MODELS";

    public StepResult Run(AgentDeclarations declarations)
    {
        if (declarations.Agents.GetValueOrDefault(AgentName) is not { } agent)
        {
            return new Error($"No agent named '{AgentName}' is declared, so nothing runs the model server.");
        }

        if (!agent.Environment.TryGetValue(StoreVariable, out var told) || HostPath.TryFrom(told) is not { IsSuccess: true } path
            || path.ValueObject.Directory.AbsolutePath != store.Directory.AbsolutePath)
        {
            return new Error($"'models.store' is {store.Directory.AbsolutePath}, but the {AgentName} agent's {StoreVariable} is "
                             + $"{(agent.Environment.TryGetValue(StoreVariable, out var value) ? $"'{value}'" : "unset")}: they must be one directory.");
        }

        log.Detail($"The server keeps its models in {store.Directory.AbsolutePath}.");
        return StepResult.Successful;
    }
}
