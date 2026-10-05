using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Agents.Models;
using Wolfe.Lab.Application.Workflows.Agents.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Packages;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Agents.Steps;

public class ResolveAgentDeclarationsTests
{
    private const string Directory = "monitoring/alloy/forwarder";

    private static readonly IOptions<LabDirectories> Directories =
        Options.Create(new LabDirectories { Root = new PhysicalDirectory("/lab/root"), Data = new PhysicalDirectory("/lab/data") });

    private static readonly AgentProcess Alloy = new()
    {
        Name = AgentName.From("alloy"),
        Package = new AgentPackage(GitHubRepository.From("grafana/alloy"), PackageVersion.From("1.20.1"), Template.From("alloy-{platform}.zip"), Template.From("SHA256SUMS")),
        Program = Template.From("{package}/alloy-{platform}"),
        Arguments = Catalogs.Templates("run", "{lab.root}/alloy/forwarder.alloy"),
        Environment = Catalogs.Variables(("DOCKER_HOST", "{node.docker}")),
        Supersedes = ["homebrew.mxcl.grafana-alloy"]
    };

    // The node is the environment's (LAB_NODE); the runner is only what ritten.json's entries are keyed by.
    private static StepResult<AgentDeclarations> Resolve(ServiceCatalog catalog, string? node, string runner = "", AgentsDeclaredPerNode? perNode = null) =>
        new ResolveAgentDeclarations(perNode ?? new AgentsDeclaredPerNode(new Dictionary<string, NodeAgentsOptions>()), new NodeRunner(runner), Directories,
                Options.Create(new LabNode { Given = node }), new WorkflowJob("agents", "deploy", DryRun: false, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(catalog, catalog.DeploymentUnitAt(RepositoryPath.From(Directory)).ShouldNotBeNull());

    private static ServiceCatalog Placed(DeploymentTarget runsOn) =>
        Catalogs.Of(Catalogs.Nodes(Catalogs.Node("mini", NodeRole.Server, docker: "unix:///var/run/docker.sock"), Catalogs.Node("studio", NodeRole.Hybrid)),
            Directory, Catalogs.Agent("forwarder", runsOn, Alloy));

    [Fact]
    public void Run_ResolvesThePlacedAgentAsItRunsOnThisNode()
    {
        var agent = Resolve(Placed(DeploymentTarget.EveryOf(NodeRole.Server)), "mini").Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem();

        agent.Key.ShouldBe("alloy");
        agent.Value.Program.ShouldBe(HostPath.From("{package}/alloy-darwin-arm64"));
        agent.Value.Arguments.ShouldBe(["run", "/lab/root/alloy/forwarder.alloy"]);
        agent.Value.Package.ShouldNotBeNull().Asset.ShouldBe("alloy-darwin-arm64.zip");
        agent.Value.Supersedes.ShouldBe(["homebrew.mxcl.grafana-alloy"]);
    }

    [Fact]
    public void Run_SetsTheNodesOwnVariables_AndPlacesItsLog()
    {
        var agent = Resolve(Placed(DeploymentTarget.EveryOf(NodeRole.Server)), "mini").Value.ShouldNotBeNull().Agents["alloy"];

        agent.Environment.ShouldBe(new Dictionary<string, string>
        {
            ["DOCKER_HOST"] = "unix:///var/run/docker.sock",
            ["LAB_HOST"] = "mini",
            ["LAB_ROLE"] = "server",
            ["LAB_ROOT"] = "/lab/root"
        });
        agent.Log.ShouldBe(HostPath.From("/lab/root/logs/monitoring-alloy-forwarder.log"));
    }

    [Fact]
    public void Run_HasNothingToDoOnANodeItIsNotPlacedOn()
    {
        var result = Resolve(Placed(DeploymentTarget.EveryOf(NodeRole.Server)), "studio");

        result.Outcome.ShouldBe(StepResult.NothingToDo);
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void Run_RefusesANodeTheLabDoesNotDeclare() =>
        Resolve(Placed(DeploymentTarget.All), "pi").Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message
            .ShouldBe("LAB_NODE is 'pi', which platform/nodes.yaml does not declare.");

    [Fact]
    public void Run_RefusesToRunWhereNoNodeIsSet() =>
        Resolve(Placed(DeploymentTarget.All), null).Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldStartWith("LAB_NODE is not set");

    [Fact]
    public void Run_TakesThisNodesEntryFromRittenJson_WhileTheComponentDeclaresNoAgent()
    {
        var catalog = Catalogs.Of(Directory, Catalogs.Definition("forwarder", ComponentKind.Collector, WorkflowName.Agents));
        var perNode = new AgentsDeclaredPerNode(new Dictionary<string, NodeAgentsOptions>
        {
            ["MacMini"] = new() { Agents = new Dictionary<string, AgentOptions> { ["alloy"] = new() { Program = HostPath.From("/opt/alloy") } } }
        });

        Resolve(catalog, node: null, runner: "MacMini", perNode).Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem().Key.ShouldBe("alloy");
    }

    [Fact]
    public void Run_RefusesAVariableTheNodeSetsForEveryAgent()
    {
        var catalog = Catalogs.Of(Catalogs.Nodes(Catalogs.Node("mini", NodeRole.Server)), Directory,
            Catalogs.Agent("forwarder", DeploymentTarget.All, Alloy with { Environment = Catalogs.Variables(("LAB_HOST", "mini")) }));

        Resolve(catalog, "mini").Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message
            .ShouldBe("monitoring/alloy/forwarder/component.yaml: environment.LAB_HOST: LAB_HOST is set for every agent by its node, and is not declared.");
    }

    [Fact]
    public void Run_RefusesANodeThatKeepsTheLabElsewhereThanTheEnvironmentSays()
    {
        var result = new ResolveAgentDeclarations(new AgentsDeclaredPerNode(new Dictionary<string, NodeAgentsOptions>()), new NodeRunner(""),
                Options.Create(new LabDirectories { Root = new PhysicalDirectory("/elsewhere/root"), Data = new PhysicalDirectory("/lab/data") }),
                Options.Create(new LabNode { Given = "mini" }), new WorkflowJob("agents", "deploy", DryRun: false, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(Placed(DeploymentTarget.All), Placed(DeploymentTarget.All).DeploymentUnitAt(RepositoryPath.From(Directory)).ShouldNotBeNull());

        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message
            .ShouldBe("mini keeps the lab in /lab/root and /lab/data by platform/nodes.yaml, but LAB_ROOT and LAB_DATA here say /elsewhere/root and /lab/data: make them one place.");
    }
}
