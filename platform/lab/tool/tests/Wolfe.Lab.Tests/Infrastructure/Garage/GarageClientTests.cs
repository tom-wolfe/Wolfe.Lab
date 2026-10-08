using Ritten.Docker;
using Wolfe.Lab.Domain.Catalog.Components.Garage;
using Wolfe.Lab.Infrastructure.Garage;
using Wolfe.Lab.Tests.Infrastructure.Resilience;

namespace Wolfe.Lab.Tests.Infrastructure.Garage;

public class GarageClientTests
{
    private readonly IDocker _docker = Substitute.For<IDocker>();

    private static readonly CommandFailedException Refused = new("connection refused", new CommandResult(1, "", "connection refused"));

    private const string Node = "7c31591e8b67225a0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f";

    [Fact]
    public async Task Layout_ReadsEachStoringNodesRoleInBytes()
    {
        _docker.Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, $$"""
            {
              "version": 3,
              "roles": [
                { "id": "{{Node}}", "zone": "home", "tags": [], "capacity": 500000000000, "storedPartitions": 256 },
                { "id": "gateway", "zone": "home", "tags": [], "capacity": null }
              ],
              "partitionSize": 1953125000,
              "stagedRoleChanges": []
            }
            """, ""));

        var layout = await Client().Layout(TestContext.Current.CancellationToken);

        layout.Version.ShouldBe(3);
        layout.RoleOf(Node).ShouldBe(new GarageLayout(GarageZone.From("home"), StorageCapacity.From(500_000_000_000)));
        layout.RoleOf("gateway").ShouldBeNull();
        var exec = _docker.ReceivedCalls().Select(call => call.GetArguments()[0]).OfType<ContainerExec>().Single();
        exec.Container.ShouldBe("garage");
        exec.Arguments.ShouldBe(["/garage", "json-api", "GetClusterLayout"]);
        exec.IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    public async Task Layout_IsVersionZeroWithNoRolesOnANewCluster()
    {
        _docker.Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult(0, """{ "version": 0, "roles": [], "partitionSize": 0, "stagedRoleChanges": [] }""", ""));

        var layout = await Client().Layout(TestContext.Current.CancellationToken);

        layout.Version.ShouldBe(0);
        layout.RoleOf(Node).ShouldBeNull();
    }

    [Fact]
    public async Task NodeId_IsTheIdWithoutItsAddress()
    {
        _docker.Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult(0, $"{Node}@127.0.0.1:3901\n", ""));

        (await Client().NodeId(TestContext.Current.CancellationToken)).ShouldBe(Node);
    }

    [Fact]
    public async Task AssignLayout_StagesTheCapacityInBytes_SoARehearsalSkipsIt()
    {
        await Client().AssignLayout(Node, new GarageLayout(GarageZone.From("home"), StorageCapacity.From(500_000_000_000)), TestContext.Current.CancellationToken);

        await _docker.Received().Exec(
            Arg.Is<ContainerExec>(exec => !exec.IsReadOnly && exec.Arguments.SequenceEqual(new[] { "/garage", "layout", "assign", "-z", "home", "-c", "500000000000", Node })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AwaitReady_AsksUntilStatusAnswers()
    {
        var attempts = 0;
        _docker.Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++attempts < 3 ? throw Refused : new CommandResult(0, "", ""));

        (await Client().AwaitReady(TestContext.Current.CancellationToken)).ShouldBeTrue();
        attempts.ShouldBe(3);
    }

    [Fact]
    public async Task AwaitReady_GivesUpOnADaemonThatNeverAnswers()
    {
        _docker.Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>()).Returns<CommandResult>(_ => throw Refused);

        (await Client(TimeSpan.FromMilliseconds(50)).AwaitReady(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    private GarageClient Client(TimeSpan? limit = null) =>
        new(_docker, Pipelines.Polling(GarageClient.Answering, limit ?? TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(1)));
}
