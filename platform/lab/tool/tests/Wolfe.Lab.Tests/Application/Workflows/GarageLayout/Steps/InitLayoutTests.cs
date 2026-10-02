using Wolfe.Lab.Application.Workflows.GarageLayout.Models;
using Wolfe.Lab.Application.Workflows.GarageLayout.Steps;
using Wolfe.Lab.Infrastructure.Garage;

namespace Wolfe.Lab.Tests.Application.Workflows.GarageLayout.Steps;

public class InitLayoutTests
{
    private readonly IGarage _garage = Substitute.For<IGarage>();
    private static readonly Layout Home = new("home", "500G");

    private InitLayout Step() => new(_garage, Home, Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_AssignsAndAppliesTheFirstLayoutOnAFreshCluster()
    {
        _garage.LayoutVersion(Arg.Any<CancellationToken>()).Returns(0);
        _garage.NodeId(Arg.Any<CancellationToken>()).Returns("7c31591e8b67225a");

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _garage.Received().AssignLayout("7c31591e8b67225a", "home", "500G", Arg.Any<CancellationToken>());
        await _garage.Received().ApplyLayout(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_LeavesAnExistingLayoutAlone()
    {
        _garage.LayoutVersion(Arg.Any<CancellationToken>()).Returns(3);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _garage.DidNotReceiveWithAnyArgs().AssignLayout("", "", "", TestContext.Current.CancellationToken);
        await _garage.DidNotReceiveWithAnyArgs().ApplyLayout(0, TestContext.Current.CancellationToken);
    }
}
