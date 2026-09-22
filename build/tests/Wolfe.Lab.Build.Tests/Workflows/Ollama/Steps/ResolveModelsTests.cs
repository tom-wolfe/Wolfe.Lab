using Wolfe.Lab.Build.Clients.Ollama;
using Wolfe.Lab.Build.Workflows.Ollama.Models;
using Wolfe.Lab.Build.Workflows.Ollama.Steps;
using OllamaModel = Wolfe.Lab.Build.Clients.Ollama.OllamaModel;

namespace Wolfe.Lab.Build.Tests.Workflows.Ollama.Steps;

public class ResolveModelsTests
{
    private readonly IOllama _ollama = Substitute.For<IOllama>();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();

    private ResolveModels Step(params string[] declared) =>
        new(new ModelPlan([.. declared.Select(OllamaModel.From)]), _ollama, _log);

    private void Installed(params string[] models) =>
        _ollama.Installed(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<OllamaModel>>([.. models.Select(OllamaModel.From)]);

    [Fact]
    public async Task Run_AsksForOnlyWhatIsMissing()
    {
        Installed("qwen3:8b");

        var result = await Step("qwen3:8b", "llama3.2:3b").Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Models.Select(m => m.Value).ShouldBe(["llama3.2:3b"]);
    }

    [Fact]
    public async Task Run_AsksForNothingWhenTheNodeIsCurrent()
    {
        Installed("qwen3:8b");

        var result = await Step("qwen3:8b").Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Models.ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_ReportsAnUndeclaredModelButNeverRemovesIt()
    {
        Installed("qwen3:8b", "someone-elses:70b");

        var result = await Step("qwen3:8b").Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Models.ShouldBeEmpty();
        _log.Received().Detail(Arg.Is<string>(m => m.Contains("someone-elses:70b")));
    }
}
