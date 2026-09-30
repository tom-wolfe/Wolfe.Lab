using Wolfe.Lab.Workflows.Ollama.Models;
using OllamaModel = Wolfe.Lab.Clients.Ollama.OllamaModel;

namespace Wolfe.Lab.Tests.Workflows.Ollama;

public class RolePlanTests
{
    [Fact]
    public void From_NamesEachRoleUnderTheLabNamespace() =>
        RolePlan.From(new Dictionary<string, ModelRole> { ["background"] = new() { Model = OllamaModel.From("qwen3:8b") } })
            .Roles.ShouldBe(new Dictionary<OllamaModel, OllamaModel>
            {
                [OllamaModel.From("lab/background:latest")] = OllamaModel.From("qwen3:8b")
            });

    [Theory]
    [InlineData("lab/background:latest", true)]
    [InlineData("qwen3:8b", false)]
    [InlineData("library/lab:latest", false)]
    public void IsAlias_IsOnlyTheLabNamespace(string model, bool expected) =>
        RolePlan.IsAlias(OllamaModel.From(model)).ShouldBe(expected);
}
