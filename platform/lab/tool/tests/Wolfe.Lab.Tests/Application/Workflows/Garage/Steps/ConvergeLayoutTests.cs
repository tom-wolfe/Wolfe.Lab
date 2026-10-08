using Wolfe.Lab.Application.Workflows.Garage.Steps;
using Wolfe.Lab.Domain.Catalog.Components.Garage;
using Wolfe.Lab.Infrastructure.Garage;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Garage.Steps;

public class ConvergeLayoutTests
{
    private const string Node = "7c31591e8b67225a";

    private static readonly GarageLayout Declared = new(GarageZone.From("home"), StorageCapacity.From(500_000_000_000));

    private readonly IGarage _garage = Substitute.For<IGarage>();

    public ConvergeLayoutTests() => _garage.NodeId(Arg.Any<CancellationToken>()).Returns(Node);

    [Fact]
    public async Task GivesANewClusterItsFirstLayout()
    {
        Current(new ClusterLayout(0, new Dictionary<string, GarageLayout>()));

        (await Converge()).IsFailure.ShouldBeFalse();

        await _garage.Received().AssignLayout(Node, Declared, Arg.Any<CancellationToken>());
        await _garage.Received().ApplyLayout(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AppliesTheNextVersionWhenTheDeclarationChanged()
    {
        Current(new ClusterLayout(2, new Dictionary<string, GarageLayout> { [Node] = Declared with { Capacity = StorageCapacity.From(250_000_000_000) } }));

        (await Converge()).IsFailure.ShouldBeFalse();

        await _garage.Received().AssignLayout(Node, Declared, Arg.Any<CancellationToken>());
        await _garage.Received().ApplyLayout(3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangesNothingWhenTheNodeHasItsRole()
    {
        Current(new ClusterLayout(2, new Dictionary<string, GarageLayout> { [Node] = Declared }));

        (await Converge()).IsFailure.ShouldBeFalse();

        _garage.ReceivedCalls().Select(call => call.GetMethodInfo().Name).ShouldBe([nameof(IGarage.NodeId), nameof(IGarage.Layout)], ignoreOrder: true);
    }

    private void Current(ClusterLayout layout) => _garage.Layout(Arg.Any<CancellationToken>()).Returns(layout);

    private Task<StepResult> Converge() =>
        new ConvergeLayout(_garage, Substitute.For<IWorkflowLog>())
            .Run(Catalogs.Unit("platform/garage/compose", Catalogs.Garage("server", "garage", Declared)), TestContext.Current.CancellationToken);
}
