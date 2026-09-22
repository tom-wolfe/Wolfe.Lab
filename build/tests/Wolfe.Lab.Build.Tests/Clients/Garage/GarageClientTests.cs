using Wolfe.Lab.Build.Clients.Garage;

namespace Wolfe.Lab.Build.Tests.Clients.Garage;

public class GarageClientTests
{
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    [Fact]
    public async Task LayoutVersion_ReadsTheCurrentVersionLine()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "==== CURRENT CLUSTER LAYOUT ====\nID  Zone  Capacity\n\nCurrent cluster layout version: 4\n", ""));

        (await new GarageClient(_commands).LayoutVersion(TestContext.Current.CancellationToken)).ShouldBe(4);
        var command = (Command)_commands.ReceivedCalls().Single().GetArguments()[0]!;
        command.Arguments.ShouldBe(["exec", "garage", "/garage", "layout", "show"]);
    }

    [Fact]
    public async Task NodeId_TakesThePrefixTheLayoutCommandsAccept()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "7c31591e8b67225a0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f\n", ""));

        (await new GarageClient(_commands).NodeId(TestContext.Current.CancellationToken)).ShouldBe("7c31591e8b67225a");
    }

    [Fact]
    public async Task IsReady_IsWhetherStatusAnswers()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(1, "", "connection refused"));

        (await new GarageClient(_commands).IsReady(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
