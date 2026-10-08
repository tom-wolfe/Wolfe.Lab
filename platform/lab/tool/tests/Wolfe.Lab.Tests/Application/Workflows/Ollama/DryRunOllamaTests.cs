using Wolfe.Lab.Infrastructure.Ollama;
using Wolfe.Lab.Tests.Infrastructure.Resilience;

namespace Wolfe.Lab.Tests.Application.Workflows.Ollama;

public class DryRunOllamaTests
{
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    private DryRunOllama Rehearsal() => new(Substitute.For<IWorkflowLog>(),
        new OllamaClient(_commands, Substitute.For<IFileSystem>(), Pipelines.Polling(OllamaClient.Serving, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1))));

    [Fact]
    public async Task AwaitServing_WaitsForNothingOnARehearsal()
    {
        (await Rehearsal().AwaitServing(TestContext.Current.CancellationToken)).ShouldBeTrue();

        await _commands.DidNotReceiveWithAnyArgs().Run(default!, TestContext.Current.CancellationToken);
    }

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
