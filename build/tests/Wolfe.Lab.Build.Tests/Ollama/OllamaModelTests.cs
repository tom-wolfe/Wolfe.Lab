using Wolfe.Lab.Build.Ollama.Models;

namespace Wolfe.Lab.Build.Tests.Ollama;

public class OllamaModelTests
{
    [Fact]
    public void From_KeepsTheTag() => OllamaModel.From("qwen3:8b").Value.ShouldBe("qwen3:8b");

    [Theory]
    [InlineData("qwen3")]
    [InlineData("qwen3:")]
    [InlineData(":8b")]
    [InlineData("qwen3:8b:extra")]
    [InlineData("qwen3 :8b")]
    public void From_RefusesAnythingNotPinnedToATag(string value) =>
        Should.Throw<Exception>(() => OllamaModel.From(value));

    [Fact]
    public void TryParse_AnswersNullForALineThatNamesNoModel()
    {
        OllamaModel.TryParse(null).ShouldBeNull();
        OllamaModel.TryParse("NAME").ShouldBeNull();
        OllamaModel.TryParse("").ShouldBeNull();
    }

    [Fact]
    public void TryParse_ReadsAName() =>
        OllamaModel.TryParse("qwen3:8b").ShouldNotBeNull().Value.ShouldBe("qwen3:8b");
}
