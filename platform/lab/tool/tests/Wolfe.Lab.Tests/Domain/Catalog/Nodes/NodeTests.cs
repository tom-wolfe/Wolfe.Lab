using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Network;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Tests.Domain.Catalog.Nodes;

public class NodeTests
{
    private static Result<Node> Create(string file = "platform/nodes.yaml", string name = "mini", string runner = "MacMini") =>
        Node.Create(new DocumentSource(RepositoryPath.From(file)), NodeName.From(name), NodeRole.Server, NodePlatform.DarwinArm64,
            HostName.From("macmini.tailnet.ts.net"), runner);

    [Fact]
    public void Create_MakesANodeDeclaredInPlatform() =>
        Create().Value.ShouldNotBeNull().Runner.ShouldBe("MacMini");

    [Fact]
    public void Create_RefusesANodeDeclaredAnywhereElse() =>
        Create("monitoring/nodes.yaml").Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("declared in platform/");

    [Theory]
    [InlineData("macmini.tailnet.ts.net")]
    [InlineData("192.168.0.2")]
    public void HostName_TakesADnsNameOrAnAddress(string host) =>
        HostName.TryFrom(host).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData("http://macmini")]
    [InlineData("mac mini")]
    public void HostName_RefusesWhatIsNotOne(string host) =>
        HostName.TryFrom(host).Error.ErrorMessage.ShouldContain("is not a host name");

    [Theory]
    [InlineData("unix:///var/run/docker.sock")]
    [InlineData("tcp://192.168.0.2:2375")]
    public void DockerHost_TakesAnEndpointDockerReads(string endpoint) =>
        DockerHost.TryFrom(endpoint).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData("/var/run/docker.sock")]
    [InlineData("http://docker:2375")]
    public void DockerHost_RefusesWhatIsNotOne(string endpoint) =>
        DockerHost.TryFrom(endpoint).Error.ErrorMessage.ShouldContain("is not a Docker endpoint");

    [Fact]
    public void Add_RefusesANodeNamedTwice()
    {
        var catalog = new ServiceCatalog();
        catalog.Add(Create().Value.ShouldNotBeNull());

        catalog.Add(Create(runner: "Other").Value.ShouldNotBeNull()).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("declared already");
    }

    [Fact]
    public void Add_RefusesARunnerTwoNodesCarry()
    {
        var catalog = new ServiceCatalog();
        catalog.Add(Create().Value.ShouldNotBeNull());

        catalog.Add(Create(name: "studio").Value.ShouldNotBeNull()).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("runner 'MacMini' too");
    }

    [Fact]
    public void NodeRunning_FindsTheNodeByItsRunner()
    {
        var catalog = new ServiceCatalog();
        var mini = catalog.Add(Create().Value.ShouldNotBeNull()).Value.ShouldNotBeNull();

        catalog.NodeRunning("MacMini").ShouldBe(mini);
        catalog.NodeRunning("MacStudio").ShouldBeNull();
    }

    [Fact]
    public void Platform_SplitsIntoItsOsAndArch()
    {
        NodePlatform.LinuxArm64.Os.ShouldBe("linux");
        NodePlatform.LinuxArm64.Arch.ShouldBe("arm64");
    }
}
