using Wolfe.Lab.Build.Clients.Ollama;

namespace Wolfe.Lab.Build.Tests.Workflows.Ollama;

public class DryRunOllamaTests
{
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    private DryRunOllama Rehearsal() => new(Substitute.For<IWorkflowLog>(), new OllamaClient(_commands));

    [Fact]
    public async Task Installed_IsNothingWhereNoServerIsRunningYet()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult(1, "", "Error: could not connect to ollama server"));

        (await Rehearsal().Installed(TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Installed_ReadsARunningServer()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult(0, "NAME        ID      SIZE    MODIFIED\nqwen3:8b    abc     5 GB    2 days ago\n", ""));

        (await Rehearsal().Installed(TestContext.Current.CancellationToken)).Select(m => m.Value).ShouldBe(["qwen3:8b"]);
    }
}
