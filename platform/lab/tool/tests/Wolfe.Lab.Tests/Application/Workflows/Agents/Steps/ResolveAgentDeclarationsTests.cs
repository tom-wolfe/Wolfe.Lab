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

public class ResolveAgentDeclarationsTests
{
    private const string Directory = "monitoring/alloy/forwarder";

    private static readonly IOptions<LabDirectories> Directories =
        Options.Create(new LabDirectories { Root = new PhysicalDirectory("/lab/root"), Data = new PhysicalDirectory("/lab/data") });

    private static readonly AgentProcess Alloy = new()
    {
        Name = AgentName.From("alloy"),
        Package = new AgentPackage("grafana/alloy", "1.20.1", "alloy-{platform}.zip", "SHA256SUMS"),
        Program = "{package}/alloy-{platform}",
        Arguments = ["run", "{lab.root}/alloy/forwarder.alloy"],
        Environment = new Dictionary<string, string> { ["DOCKER_HOST"] = "{node.docker}" },
        Supersedes = ["homebrew.mxcl.grafana-alloy"]
    };

    private static StepResult<AgentDeclarations> Resolve(ServiceCatalog catalog, string runner, AgentsDeclaredPerNode? perNode = null) =>
        new ResolveAgentDeclarations(perNode ?? new AgentsDeclaredPerNode(new Dictionary<string, NodeAgentsOptions>()), new NodeRunner(runner), Directories,
                new WorkflowJob("agents", "deploy", DryRun: false, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(catalog, catalog.DeploymentUnitAt(RepositoryPath.From(Directory)).ShouldNotBeNull());

    private static ServiceCatalog Placed(DeploymentTarget runsOn) =>
        Catalogs.Of(Catalogs.Nodes(Catalogs.Node("mini", NodeRole.Server, docker: "unix:///var/run/docker.sock"), Catalogs.Node("studio", NodeRole.Hybrid)),
            Directory, Catalogs.Agent("forwarder", runsOn, Alloy));

    [Fact]
    public void Run_ResolvesThePlacedAgentAsItRunsOnThisNode()
    {
        var agent = Resolve(Placed(DeploymentTarget.EveryOf(NodeRole.Server)), "Mini").Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem();

        agent.Key.ShouldBe("alloy");
        agent.Value.Program.ShouldBe(HostPath.From("{package}/alloy-darwin-arm64"));
        agent.Value.Arguments.ShouldBe(["run", "/lab/root/alloy/forwarder.alloy"]);
        agent.Value.Package.ShouldNotBeNull().Asset.ShouldBe("alloy-darwin-arm64.zip");
        agent.Value.Supersedes.ShouldBe(["homebrew.mxcl.grafana-alloy"]);
    }

    [Fact]
    public void Run_SetsTheNodesOwnVariables_AndPlacesItsLog()
    {
        var agent = Resolve(Placed(DeploymentTarget.EveryOf(NodeRole.Server)), "Mini").Value.ShouldNotBeNull().Agents["alloy"];

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
        var result = Resolve(Placed(DeploymentTarget.EveryOf(NodeRole.Server)), "Studio");

        result.Outcome.ShouldBe(StepResult.NothingToDo);
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void Run_RefusesARunnerNoNodeCarries() =>
        Resolve(Placed(DeploymentTarget.All), "wolfe-pi5").Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("runner 'wolfe-pi5'");

    [Fact]
    public void Run_TakesThisNodesEntryFromRittenJson_WhileTheComponentDeclaresNoAgent()
    {
        var catalog = Catalogs.Of(Directory, Catalogs.Definition("forwarder", ComponentKind.Collector, WorkflowName.Agents));
        var perNode = new AgentsDeclaredPerNode(new Dictionary<string, NodeAgentsOptions>
        {
            ["MacMini"] = new() { Agents = new Dictionary<string, AgentOptions> { ["alloy"] = new() { Program = HostPath.From("/opt/alloy") } } }
        });

        Resolve(catalog, "MacMini", perNode).Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem().Key.ShouldBe("alloy");
    }
}
