using Wolfe.Lab.Build.Ollama;

namespace Wolfe.Lab.Build.Tests.Ollama;

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
}
