using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Application.Workflows.Ollama.Steps;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Infrastructure.Ollama;
using OllamaModel = Wolfe.Lab.Infrastructure.Ollama.OllamaModel;

namespace Wolfe.Lab.Tests.Application.Workflows.Ollama.Steps;

public class ResolveModelsTests
{
    private readonly IOllama _ollama = Substitute.For<IOllama>();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();

    private ResolveModels Step() => new(_ollama, _log);

    // The server on this node, serving each model for a use of its own.
    private static ServerPlan Plan(params string[] models) =>
        new(ComponentName.From("mini"), [.. models.Select((model, index) => new ServedModel(OllamaModel.From($"lab/use-{index}:latest"), OllamaModel.From(model), null))], []);

    private void Installed(params string[] models) =>
        _ollama.Installed(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<OllamaModel>>([.. models.Select(OllamaModel.From)]);

    [Fact]
    public async Task Run_AsksForOnlyWhatIsMissing()
    {
        Installed("qwen3:8b");

        var result = await Step().Run(Plan("qwen3:8b", "llama3.2:3b"), TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Models.Select(m => m.Value).ShouldBe(["llama3.2:3b"]);
    }

    [Fact]
    public async Task Run_AsksForNothingWhenTheNodeIsCurrent()
    {
        Installed("qwen3:8b");

        var result = await Step().Run(Plan("qwen3:8b"), TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Models.ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_ReportsAnUndeclaredModelButNeverRemovesIt()
    {
        Installed("qwen3:8b", "someone-elses:70b");

        var result = await Step().Run(Plan("qwen3:8b"), TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Models.ShouldBeEmpty();
        _log.Received().Detail(Arg.Is<string>(m => m.Contains("someone-elses:70b")));
    }

    [Fact]
    public async Task Run_DoesNotReportAUsesNameAsUndeclared()
    {
        Installed("qwen3:8b", "lab/background:latest");

        await Step().Run(Plan("qwen3:8b"), TestContext.Current.CancellationToken);

        _log.DidNotReceive().Detail(Arg.Is<string>(m => m.Contains("lab/background")));
    }
}
