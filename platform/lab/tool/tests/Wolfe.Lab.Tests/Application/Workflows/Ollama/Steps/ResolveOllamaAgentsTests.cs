using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Application.Workflows.Ollama.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Tests.Application.Agents;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Ollama.Steps;

public class ResolveOllamaAgentsTests
{
    private const string Directory = "ai/ollama/mini";

    private static readonly IReadOnlyDictionary<string, AgentOptions> InRittenJson = new Dictionary<string, AgentOptions>
    {
        ["ollama"] = new() { Program = HostPath.From("/opt/ollama") }
    };

    private static StepResult<AgentDeclarations> Resolve(ServiceCatalog catalog, string node = "mini") =>
        new ResolveOllamaAgents(new OllamaAgents(InRittenJson), Resolvers.On(node), new WorkflowJob("ollama", "deploy", DryRun: false, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(catalog, catalog.DeploymentUnitAt(RepositoryPath.From(Directory)).ShouldNotBeNull());

    private static ServiceCatalog Declared() =>
        Catalogs.Of(Catalogs.Nodes(Catalogs.Node("mini", NodeRole.Server), Catalogs.Node("studio", NodeRole.Hybrid)), Directory,
            new Catalogs.Declaration("mini", ComponentKind.Model, WorkflowName.Ollama, RunsOn: DeploymentTarget.On([NodeName.From("mini")]).Value,
                Agent: new AgentProcess { Name = AgentName.From("ollama"), Program = Template.From("{package}/ollama"), Arguments = Catalogs.Templates("serve") }));

    [Fact]
    public void Run_TakesTheAgentTheCatalogDeclares() =>
        Resolve(Declared()).Value.ShouldNotBeNull().Agents["ollama"].Arguments.ShouldBe(["serve"]);

    [Fact]
    public void Run_HasNothingToDoOnAnotherNode() =>
        Resolve(Declared(), "studio").Outcome.ShouldBe(StepResult.NothingToDo);

    [Fact]
    public void Run_TakesRittenJsonsAgent_WhileTheComponentDeclaresNone() =>
        Resolve(Catalogs.Of(Directory, Catalogs.Definition("mini", ComponentKind.Model, WorkflowName.Ollama))).Value.ShouldNotBeNull()
            .Agents["ollama"].Program.ShouldBe(HostPath.From("/opt/ollama"));
}
