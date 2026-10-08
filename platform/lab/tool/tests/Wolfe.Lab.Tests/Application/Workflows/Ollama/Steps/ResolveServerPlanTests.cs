using Microsoft.Extensions.Options;
using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Application.Workflows.Ollama.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Components.Models;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;
using OllamaModel = Wolfe.Lab.Infrastructure.Ollama.OllamaModel;

namespace Wolfe.Lab.Tests.Application.Workflows.Ollama.Steps;

public class ResolveServerPlanTests
{
    private static AgentProcess Ollama => new() { Name = AgentName.From("ollama"), Program = Template.From("/usr/bin/ollama") };

    // The mini's server and the studio's, and two uses: interactive, which the mini runs smaller,
    // and embedding, the same everywhere.
    private static ServiceCatalog Catalog()
    {
        var catalog = Catalogs.Of(Catalogs.Nodes(Catalogs.Node("mini", NodeRole.Server), Catalogs.Node("studio", NodeRole.Hybrid), Catalogs.Node("pi", NodeRole.Server)),
            "ai/ollama/mini", Catalogs.Agent("mini", On("mini"), Ollama));
        var service = catalog.Services.Single();
        Catalogs.Add(service, Source("studio"), Catalogs.Agent("studio", On("studio"), Ollama)).Value.ShouldNotBeNull();
        Catalogs.Add(service, Source("interactive"), Catalogs.Model("interactive", "qwen3.6:35b-a3b", "mini", "studio") with
        {
            Serving = new Catalogs.Use("qwen3.6:35b-a3b", 16384, [("mini", "qwen3.5:9b", 8192), ("studio", null, null)])
        }).Value.ShouldNotBeNull();
        Catalogs.Add(service, Source("embedding"), Catalogs.Model("embedding", "embeddinggemma:300m", "mini", "studio")).Value.ShouldNotBeNull();
        return catalog;
    }

    private static DeploymentTarget On(string node) => DeploymentTarget.On([NodeName.From(node)]).Value.ShouldNotBeNull();

    private static DocumentSource Source(string directory) => new(RepositoryPath.From($"ai/ollama/{directory}/component.yaml"));

    private static StepResult<ServerPlan> Resolve(string? node, string directory = "ai/ollama/interactive")
    {
        var catalog = Catalog();
        return new ResolveServerPlan(Options.Create(new LabNode { Given = node }), Substitute.For<IWorkflowLog>())
            .Run(catalog, catalog.DeploymentUnitAt(RepositoryPath.From(directory)).ShouldNotBeNull().Value.ShouldNotBeNull());
    }

    [Fact]
    public void Run_ServesWhatThisNodesServerRunsForTheUse()
    {
        var plan = Resolve("mini").Value.ShouldNotBeNull();

        plan.Server.ShouldBe(ComponentName.From("mini"));
        plan.Served.ShouldBe([new ServedModel(OllamaModel.From("lab/interactive:latest"), OllamaModel.From("qwen3.5:9b"), ContextLength.From(8192))]);
        plan.Uses.ShouldBe([OllamaModel.From("lab/embedding:latest"), OllamaModel.From("lab/interactive:latest")], ignoreOrder: true);
    }

    [Fact]
    public void Run_TakesTheDefaultsWhereTheServerSaysNothing() =>
        Resolve("studio").Value.ShouldNotBeNull().Served.ShouldBe([new ServedModel(OllamaModel.From("lab/interactive:latest"), OllamaModel.From("qwen3.6:35b-a3b"), ContextLength.From(16384))]);

    [Fact]
    public void Run_ServesNothingOnANodeWithoutAServer() =>
        Resolve("pi").Value.ShouldBe(ServerPlan.None);

    [Fact]
    public void Run_ServesNothingForADeploymentOfNoModels() =>
        Resolve("mini", "ai/ollama/mini").Value.ShouldBe(ServerPlan.None);

    [Fact]
    public void Run_RefusesToRunWhereNoNodeIsSet() =>
        Resolve(null).Outcome.IsFailure.ShouldBeTrue();
}
