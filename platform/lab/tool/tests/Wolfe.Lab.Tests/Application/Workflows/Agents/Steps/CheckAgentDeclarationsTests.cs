using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Agents.Models;
using Wolfe.Lab.Application.Workflows.Agents.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Agents.Steps;

public class CheckAgentDeclarationsTests
{
    private const string Directory = "monitoring/beszel/agent";

    private static StepResult Check(ServiceCatalog catalog, AgentsDeclaredPerNode? perNode = null) =>
        new CheckAgentDeclarations(perNode ?? new AgentsDeclaredPerNode(new Dictionary<string, NodeAgentsOptions>()),
                new WorkflowJob("agents", "check", DryRun: false, AutoApprove: false), Substitute.For<IWorkflowLog>())
            .Run(catalog, catalog.DeploymentUnitAt(RepositoryPath.From(Directory)).ShouldNotBeNull());

    // While ritten.json declares the agents per node: the component declares none of its own.
    private static StepResult CheckPerNode(params (string Node, string Agent, AgentOptions Options)[] agents)
    {
        var nodes = agents
            .GroupBy(a => a.Node)
            .ToDictionary(g => g.Key, g => new NodeAgentsOptions { Agents = g.ToDictionary(a => a.Agent, a => a.Options) });
        return Check(Catalogs.Of(Directory, Catalogs.Definition("agent", ComponentKind.Collector, WorkflowName.Agents)), new AgentsDeclaredPerNode(nodes));
    }

    private static AgentOptions Beszel(string token = "op://Wolfe.Lab/beszel-agent/credential") => new()
    {
        Program = HostPath.From("/opt/homebrew/opt/beszel-agent/bin/beszel-agent"),
        Environment = new Dictionary<string, string> { ["TOKEN"] = token, ["HUB_URL"] = "http://localhost:8090" }
    };

    private static AgentProcess Placed(params (string Variable, string Value)[] environment) => new()
    {
        Name = AgentName.From("beszel-agent"),
        Program = Template.From("{package}/beszel-agent"),
        Environment = Catalogs.Variables(environment)
    };

    private static StepResult CheckPlaced(AgentProcess agent, params Node[] nodes) =>
        Check(Catalogs.Of(Catalogs.Nodes(nodes), Directory, Catalogs.Agent("agent", DeploymentTarget.All, agent)));

    [Fact]
    public void Run_PassesSoundDeclarations() =>
        CheckPerNode(("MacMini", "beszel-agent", Beszel()), ("wolfe-pi5", "beszel-agent", Beszel())).IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_RefusesAVaultReferenceThatIsNotOne()
    {
        var result = CheckPerNode(("MacMini", "beszel-agent", Beszel(token: "op://Wolfe.Lab/beszel-agent")));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("TOKEN");
    }

    [Fact]
    public void Run_RefusesANameThatCannotBeALabel() =>
        CheckPerNode(("MacMini", "beszel.agent", Beszel())).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("cannot name an agent");

    [Fact]
    public void Run_RefusesAnAgentWithNoProgram() =>
        CheckPerNode(("MacMini", "beszel-agent", new AgentOptions())).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("'program'");

    [Fact]
    public void Run_NamesTheNodeOfEveryProblem() =>
        CheckPerNode(("MacMini", "beszel-agent", new AgentOptions()), ("wolfe-pi5", "beszel-agent", new AgentOptions()))
            .Errors.ShouldNotBeNull().Select(e => e.Message.Split(':')[0]).ShouldBe(["MacMini", "wolfe-pi5"]);

    [Fact]
    public void Run_RefusesAHomePathInTheEnvironment()
    {
        var agent = Beszel() with { Environment = new Dictionary<string, string> { ["DATA"] = "~/.local/share/thing" } };

        CheckPerNode(("MacStudio", "beszel-agent", agent)).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("DATA");
    }

    [Fact]
    public void Run_RefusesAComponentThatDeclaresNoAgentWhereRittenJsonDeclaresNoNodes() =>
        CheckPerNode().Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("declare its runsOn");

    [Fact]
    public void Run_PassesAPlacedAgentOnEveryNodeItRunsOn() =>
        CheckPlaced(Placed(("DOCKER_HOST", "{node.docker}")),
                Catalogs.Node("mini", NodeRole.Server, docker: "unix:///var/run/docker.sock"),
                Catalogs.Node("pi", NodeRole.Server, NodePlatform.LinuxArm64, docker: "unix:///var/run/docker.sock"))
            .IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_RefusesAPlaceholderANodeHasNothingFor_NamingTheNode()
    {
        var result = CheckPlaced(Placed(("DOCKER_HOST", "{node.docker}")),
            Catalogs.Node("mini", NodeRole.Server, docker: "unix:///var/run/docker.sock"), Catalogs.Node("studio", NodeRole.Hybrid));

        var error = result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message;
        error.ShouldStartWith("studio: ");
        error.ShouldContain("{node.docker}");
    }

    [Fact]
    public void Run_RefusesAPlacedAgentsVaultReferenceThatIsNotOne() =>
        CheckPlaced(Placed(("TOKEN", "op://Wolfe.Lab/beszel-agent")), Catalogs.Node("mini", NodeRole.Server))
            .Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldBe("mini: agent 'beszel-agent' sets TOKEN to 'op://Wolfe.Lab/beszel-agent', which is not op://<vault>/<item>/<field>.");
}
