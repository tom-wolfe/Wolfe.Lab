using Wolfe.Lab.Build.Clients.Ollama;
using Wolfe.Lab.Build.Tests.Clients.Resilience;

namespace Wolfe.Lab.Build.Tests.Workflows.Ollama;

public class OllamaClientTests
{
    private const string Listing = """
        NAME             ID              SIZE      MODIFIED
        qwen3:8b         500a1f067a9f    5.2 GB    2 days ago
        llama3.2:3b      a80c4f17acd5    2.0 GB    3 weeks ago

        """;

    [Fact]
    public void Parse_TakesTheNamesAndSkipsTheHeader() =>
        OllamaClient.Parse(Listing).Select(m => m.Value).ShouldBe(["qwen3:8b", "llama3.2:3b"]);

    [Fact]
    public void Parse_IsEmptyWhenTheNodeHoldsNothing() =>
        OllamaClient.Parse("NAME    ID    SIZE    MODIFIED\n").ShouldBeEmpty();

    [Fact]
    public void ParseIdentities_KeepsEachNamesId() =>
        OllamaClient.ParseIdentities(Listing).ShouldBe(new Dictionary<OllamaModel, string>
        {
            [OllamaModel.From("qwen3:8b")] = "500a1f067a9f",
            [OllamaModel.From("llama3.2:3b")] = "a80c4f17acd5"
        });

    [Fact]
    public async Task AwaitServing_AsksUntilTheServerAnswers()
    {
        var commands = Substitute.For<ICommandRunner>();
        commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(
            new CommandResult(1, "", "could not connect"), new CommandResult(0, Listing, ""));

        var client = new OllamaClient(commands, Pipelines.Polling(OllamaClient.Serving, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(1)));

        (await client.AwaitServing(TestContext.Current.CancellationToken)).ShouldBeTrue();
        await commands.Received(2).Run(Arg.Any<Command>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AwaitServing_GivesUpOnAServerThatNeverAnswers()
    {
        var commands = Substitute.For<ICommandRunner>();
        commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(1, "", "could not connect"));

        var client = new OllamaClient(commands, Pipelines.Polling(OllamaClient.Serving, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(1)));

        (await client.AwaitServing(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
