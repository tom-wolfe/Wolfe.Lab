using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Packages;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Tests.Domain.Catalog.Components.Agents;

public class AgentComponentTests
{
    private const string Directory = "monitoring/alloy/forwarder";

    private static readonly Node Mini = Catalogs.Node("mini", NodeRole.Server, docker: "unix:///Users/me/.docker/run/docker.sock", drives: ["/Volumes/Data1", "/Volumes/Data2"]);
    private static readonly Node Pi = Catalogs.Node("pi", NodeRole.Server, NodePlatform.LinuxArm64, docker: "unix:///var/run/docker.sock");

    private static AgentProcess Alloy(string program = "{package}/alloy-{platform}", params (string Variable, string Value)[] environment) => new()
    {
        Name = AgentName.From("alloy"),
        Package = new AgentPackage(GitHubRepository.From("grafana/alloy"), PackageVersion.From("1.20.1"), Template.From("alloy-{platform}.zip"), Template.From("SHA256SUMS")),
        Program = Template.From(program),
        Arguments = Catalogs.Templates("run", "{lab.root}/alloy/forwarder.alloy", "--storage.path={state}"),
        Environment = Catalogs.Variables(environment)
    };

    private static Result<AgentComponent> Create(AgentProcess agent, DeploymentTarget? runsOn = null) =>
        AgentComponent.Create(new DocumentSource(RepositoryPath.From($"{Directory}/component.yaml")), ComponentName.From("forwarder"),
            ComponentKind.Collector, WorkflowName.Agents, null, [], runsOn ?? DeploymentTarget.All, agent);

    private static AgentComponent Placed(AgentProcess agent, DeploymentTarget? runsOn = null, params Node[] nodes)
    {
        var catalog = Catalogs.Of(Catalogs.Nodes(nodes.Length > 0 ? nodes : [Mini, Pi]), Directory, Catalogs.Agent("forwarder", runsOn ?? DeploymentTarget.All, agent));
        return catalog.DeploymentUnitAt(RepositoryPath.From(Directory)).ShouldNotBeNull().Value.ShouldNotBeNull().Components.ShouldHaveSingleItem().ShouldBeOfType<AgentComponent>();
    }

    private static IReadOnlyList<string> Refused(AgentProcess agent, DeploymentTarget? runsOn = null, params Node[] nodes)
    {
        var service = Catalogs.AddService(Catalogs.Nodes(nodes.Length > 0 ? nodes : [Mini, Pi]), "monitoring/alloy").Value.ShouldNotBeNull();
        var component = Create(agent, runsOn).Value.ShouldNotBeNull();
        return [.. service.Add(component).Errors.ShouldNotBeNull().Select(error => error.Message)];
    }

    [Fact]
    public void Create_RefusesAPlaceholderTheLabDoesNotHave() =>
        Create(Alloy(program: "{package}/alloy-{os}")).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message
            .ShouldStartWith("monitoring/alloy/forwarder/component.yaml: program: {os} is not a placeholder here");

    [Fact]
    public void Create_RefusesAPackagePlaceholderOnlyTheProcessHas() =>
        Create(Alloy() with { Package = new AgentPackage(GitHubRepository.From("grafana/alloy"), PackageVersion.From("1.20.1"), Template.From("alloy-{node.name}.zip"), null) })
            .Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("package.asset: {node.name} is not a placeholder here");

    [Fact]
    public void Add_RefusesAPlacementOnANodeTheLabDoesNotDeclare() =>
        Refused(Alloy(), DeploymentTarget.On([NodeName.From("studio")]).Value).ShouldHaveSingleItem().ShouldContain("runsOn: names the node 'studio'");

    [Fact]
    public void Add_RefusesAPlacementThatTakesInNoNode() =>
        Refused(Alloy(), DeploymentTarget.EveryOf(NodeRole.Hybrid)).ShouldHaveSingleItem().ShouldContain("'every hybrid' takes in no node");

    [Fact]
    public void Add_RefusesTheAddressOfANodeTheLabDoesNotDeclare() =>
        Refused(Alloy(environment: ("LAB_GATEWAY", "{node.studio.address}"))).ShouldHaveSingleItem().ShouldContain("environment.LAB_GATEWAY: names the node 'studio'");

    [Fact]
    public void Add_LeavesARefusedComponentNoServicesAtAll()
    {
        var service = Catalogs.AddService(Catalogs.Nodes(Mini, Pi), "monitoring/alloy").Value.ShouldNotBeNull();
        var component = Create(Alloy(), DeploymentTarget.EveryOf(NodeRole.Hybrid)).Value.ShouldNotBeNull();

        service.Add(component).IsError.ShouldBeTrue();

        service.Components.ShouldBeEmpty();
        Should.Throw<InvalidOperationException>(() => component.Service);
    }

    [Fact]
    public void On_WritesInTheNodesFacts()
    {
        var component = Placed(Alloy(environment: [("DOCKER_HOST", "{node.docker}"), ("LAB_GATEWAY", "{node.mini.address}"), ("DRIVES", "{node.drives}")]));

        var process = component.RunningOn(Mini).Value.ShouldNotBeNull();

        process.Arguments.ShouldBe(Catalogs.Templates("run", "/lab/root/alloy/forwarder.alloy", "--storage.path=/lab/data/alloy"));
        Catalogs.Written(process.Environment).ShouldBe(new Dictionary<string, string>
        {
            ["DOCKER_HOST"] = "unix:///Users/me/.docker/run/docker.sock",
            ["LAB_GATEWAY"] = "mini.tailnet.ts.net",
            ["DRIVES"] = "/Volumes/Data1,/Volumes/Data2"
        });
    }

    [Fact]
    public void On_LeavesThePackageAndItsVersionForTheInstall()
    {
        var process = Placed(Alloy()).RunningOn(Pi).Value.ShouldNotBeNull();

        process.Program.ShouldBe(Template.From("{package}/alloy-linux-arm64"));
        process.Package.ShouldNotBeNull().Asset.ShouldBe(Template.From("alloy-linux-arm64.zip"));
    }

    [Fact]
    public void On_RefusesAFactTheNodeDoesNotHave()
    {
        var bare = Catalogs.Node("studio", NodeRole.Hybrid);
        var component = Placed(Alloy(environment: ("DOCKER_HOST", "{node.docker}")), nodes: [Mini, bare]);

        component.RunningOn(bare).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message
            .ShouldContain("environment.DOCKER_HOST: {node.docker} has no value on studio");
    }
}
