using Ritten.Docker;
using Wolfe.Lab.Infrastructure.Garage;
using Wolfe.Lab.Tests.Infrastructure.Resilience;

namespace Wolfe.Lab.Tests.Infrastructure.Garage;

public class GarageClientTests
{
    private readonly IDocker _docker = Substitute.For<IDocker>();

    private static readonly CommandFailedException Refused = new("connection refused", new CommandResult(1, "", "connection refused"));

    [Fact]
    public async Task LayoutVersion_ReadsTheCurrentVersionLine()
    {
        _docker.Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult(0, "==== CURRENT CLUSTER LAYOUT ====\nID  Zone  Capacity\n\nCurrent cluster layout version: 4\n", ""));

        (await Client().LayoutVersion(TestContext.Current.CancellationToken)).ShouldBe(4);
        var exec = _docker.ReceivedCalls().Select(call => call.GetArguments()[0]).OfType<ContainerExec>().Single();
        exec.Container.ShouldBe("garage");
        exec.Arguments.ShouldBe(["/garage", "layout", "show"]);
        exec.IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    public async Task NodeId_TakesThePrefixTheLayoutCommandsAccept()
    {
        _docker.Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult(0, "7c31591e8b67225a0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f\n", ""));

        (await Client().NodeId(TestContext.Current.CancellationToken)).ShouldBe("7c31591e8b67225a");
    }

    [Fact]
    public async Task AssignLayout_ChangesTheLayout_SoARehearsalSkipsIt()
    {
        await Client().AssignLayout("7c31591e8b67225a", "mini", "1T", TestContext.Current.CancellationToken);

        await _docker.Received().Exec(
            Arg.Is<ContainerExec>(exec => !exec.IsReadOnly && exec.Arguments.SequenceEqual(new[] { "/garage", "layout", "assign", "-z", "mini", "-c", "1T", "7c31591e8b67225a" })),
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
