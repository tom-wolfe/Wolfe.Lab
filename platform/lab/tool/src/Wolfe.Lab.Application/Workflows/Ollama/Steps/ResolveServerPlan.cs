using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Components.Models;
using Wolfe.Lab.Infrastructure.Ollama;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.Ollama.Steps;

/// <summary>
/// Works out what this node's server serves: the service's server placed on this node, and for
/// each model, the model and context that server runs it with.
/// </summary>
[Step("resolve server", StepKind.Work)]
internal sealed class ResolveServerPlan(IOptions<LabNode> node, IWorkflowLog log)
{
    public StepResult<ServerPlan> Run(ServiceCatalog catalog, DeploymentUnit unit)
    {
        var models = unit.Components.OfType<ModelComponent>().ToList();
        if (models.Count == 0)
        {
            log.Detail($"{unit} declares no models.");
            return ServerPlan.None;
        }

        if (node.Value.Name is not { } here)
        {
            return new Error($"Nothing says which node this is: set {LabNode.Variable}, as chezmoi does on every node.");
        }

        var servers = unit.Service.Components.OfType<AgentComponent>()
            .Where(server => models.Any(model => model.ServedBy.ContainsKey(server.Name)) && server.RunsOn.In(catalog).Any(placed => placed.Name == here))
            .ToList();
        switch (servers)
        {
            case []:
                log.Detail($"No server of {unit.Service} runs on {here}: nothing to serve here.");
                return ServerPlan.None;
            case [var server]:
                // The catalog refuses a server with no model for a use, so each has one.
                var served = models
                    .SelectMany(IEnumerable<ServedModel> (model) => model.ModelOn(server.Name) is { } runs
                        ? [new ServedModel(OllamaModel.From($"{model.Alias}:latest"), OllamaModel.From(runs.Value), model.ContextOn(server.Name))]
                        : [])
                    .ToList();
                log.Detail($"{server.Name} serves {string.Join(", ", served.Select(model => $"{model.Alias.Value} as {model.Model.Value}"))}.");
                return new ServerPlan(server.Name, served,
                    [.. unit.Service.Components.OfType<ModelComponent>().Select(model => OllamaModel.From($"{model.Alias}:latest"))]);
            default:
                return new Error($"{servers.Count} servers of {unit.Service} run on {here} ({string.Join(", ", servers.Select(server => server.Name))}): a node answers on one port, so it runs one.");
        }
    }
}
