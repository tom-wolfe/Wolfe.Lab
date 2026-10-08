using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Application.Workflows.Ollama.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Packages;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Tests.Application.Agents;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Ollama.Steps;

public class ResolveServerAgentTests
{
    // The mini's server, from its pinned release, and a use it serves.
    private static readonly AgentProcess Ollama = new()
    {
        Name = AgentName.From("ollama"),
        Package = new AgentPackage(GitHubRepository.From("ollama/ollama"), PackageVersion.From("0.40.1"), Template.From("ollama-darwin.tgz"), Template.From("sha256sum.txt")),
        Program = Template.From("{package}/ollama"),
        Arguments = Catalogs.Templates("serve")
    };

    private static ServiceCatalog Catalog()
    {
        var catalog = Catalogs.Of(Catalogs.Nodes(Catalogs.Node("mini", NodeRole.Server)), "ai/ollama/mini",
            Catalogs.Agent("mini", DeploymentTarget.On([NodeName.From("mini")]).Value.ShouldNotBeNull(), Ollama));
        Catalogs.Add(catalog.Services.Single(), new DocumentSource(RepositoryPath.From("ai/ollama/interactive/component.yaml")),
            Catalogs.Model("interactive", "qwen3.5:9b", "mini")).Value.ShouldNotBeNull();
        return catalog;
    }

    private static StepResult<AgentDeclarations> Resolve(ServerPlan plan)
    {
        var catalog = Catalog();
        return new ResolveServerAgent(Resolvers.On("mini"), Substitute.For<IWorkflowLog>())
            .Run(catalog, catalog.DeploymentUnitAt(RepositoryPath.From("ai/ollama/interactive")).ShouldNotBeNull().Value.ShouldNotBeNull(), plan);
    }

    [Fact]
    public void Run_IsTheServersAgent_ForItsPackageToBeInstalled()
    {
        var agent = Resolve(new ServerPlan(ComponentName.From("mini"), [], [])).Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem();

        agent.Key.ShouldBe("ollama");
        agent.Value.Package.ShouldNotBeNull().Version.ShouldBe("0.40.1");
    }

    [Fact]
    public void Run_IsNothingOnANodeWithoutAServer() =>
        Resolve(ServerPlan.None).Value.ShouldNotBeNull().Agents.ShouldBeEmpty();
}
