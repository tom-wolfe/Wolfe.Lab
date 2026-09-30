using Wolfe.Lab.Clients.Garage;
using Wolfe.Lab.Tests.Clients.Resilience;

namespace Wolfe.Lab.Tests.Clients.Garage;

public class GarageClientTests
{
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    [Fact]
    public async Task LayoutVersion_ReadsTheCurrentVersionLine()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "==== CURRENT CLUSTER LAYOUT ====\nID  Zone  Capacity\n\nCurrent cluster layout version: 4\n", ""));

        (await Client().LayoutVersion(TestContext.Current.CancellationToken)).ShouldBe(4);
        var command = (Command)_commands.ReceivedCalls().Single().GetArguments()[0]!;
        command.Arguments.ShouldBe(["exec", "garage", "/garage", "layout", "show"]);
    }

    [Fact]
    public async Task NodeId_TakesThePrefixTheLayoutCommandsAccept()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "7c31591e8b67225a0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f\n", ""));

        (await Client().NodeId(TestContext.Current.CancellationToken)).ShouldBe("7c31591e8b67225a");
    }

    [Fact]
    public async Task AwaitReady_AsksUntilStatusAnswers()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(
            new CommandResult(1, "", "connection refused"), new CommandResult(1, "", "connection refused"), new CommandResult(0, "", ""));

        (await Client().AwaitReady(TestContext.Current.CancellationToken)).ShouldBeTrue();
        await _commands.Received(3).Run(Arg.Is<Command>(c => c.Arguments.Contains("status")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AwaitReady_GivesUpOnADaemonThatNeverAnswers()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(1, "", "connection refused"));

        (await Client(TimeSpan.FromMilliseconds(50)).AwaitReady(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    private GarageClient Client(TimeSpan? limit = null) =>
        new(_commands, Pipelines.Polling(GarageClient.Answering, limit ?? TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(1)));
}
