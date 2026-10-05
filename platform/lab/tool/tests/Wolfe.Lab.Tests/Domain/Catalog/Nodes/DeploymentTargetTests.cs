using Wolfe.Lab.Domain.Catalog.Nodes;

namespace Wolfe.Lab.Tests.Domain.Catalog.Nodes;

public class DeploymentTargetTests
{
    private static readonly Node Mini = Catalogs.Node("mini", NodeRole.Server);
    private static readonly Node Studio = Catalogs.Node("studio", NodeRole.Hybrid);

    [Fact]
    public void Rule_All_TakesInEveryNode()
    {
        var placement = DeploymentTarget.Rule("all").Value.ShouldNotBeNull();

        Catalogs.Nodes(Mini, Studio).Nodes.Where(placement.Includes).ShouldBe([Mini, Studio]);
    }

    [Fact]
    public void Rule_EveryOfARole_TakesInThatRolesNodesAlone() =>
        Catalogs.Nodes(Mini, Studio).Nodes.Where(DeploymentTarget.Rule("every server").Value.ShouldNotBeNull().Includes).ShouldBe([Mini]);

    [Theory]
    [InlineData("every desktop")]
    [InlineData("mini")]
    [InlineData("everywhere")]
    [InlineData("every node")]
    public void Rule_RefusesWhatIsNotARule(string rule) =>
        DeploymentTarget.Rule(rule).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("is not a deployment target");

    [Fact]
    public void On_TakesInTheNodesItNames() =>
        Catalogs.Nodes(Mini, Studio).Nodes.Where(DeploymentTarget.On([NodeName.From("studio")]).Value.ShouldNotBeNull().Includes).ShouldBe([Studio]);

    [Fact]
    public void On_RefusesNoNodesOrOneTwice()
    {
        DeploymentTarget.On([]).IsError.ShouldBeTrue();
        DeploymentTarget.On([NodeName.From("mini"), NodeName.From("mini")]).IsError.ShouldBeTrue();
    }

    [Fact]
    public void ToString_ReadsAsItIsWritten()
    {
        DeploymentTarget.All.ToString().ShouldBe("all");
        DeploymentTarget.EveryOf(NodeRole.Server).ToString().ShouldBe("every server");
        DeploymentTarget.On([NodeName.From("mini"), NodeName.From("pi")]).Value.ShouldNotBeNull().ToString().ShouldBe("[mini, pi]");
    }
}
