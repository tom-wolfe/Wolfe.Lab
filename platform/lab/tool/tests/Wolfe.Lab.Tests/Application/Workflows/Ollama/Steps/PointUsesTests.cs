using OllamaModel = Wolfe.Lab.Infrastructure.Ollama.OllamaModel;
using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Application.Workflows.Ollama.Steps;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Models;
using Wolfe.Lab.Infrastructure.Ollama;

namespace Wolfe.Lab.Tests.Application.Workflows.Ollama.Steps;

public class PointUsesTests
{
    private static readonly OllamaModel Interactive = OllamaModel.From("lab/interactive:latest");
    private static readonly OllamaModel Embedding = OllamaModel.From("lab/embedding:latest");
    private static readonly OllamaModel Large = OllamaModel.From("qwen3.6:35b-a3b");

    private readonly IOllama _ollama = Substitute.For<IOllama>();

    public PointUsesTests()
    {
        _ollama.Installed(Arg.Any<CancellationToken>()).Returns<IReadOnlyList<OllamaModel>>([Large]);
        Holds(Large, "sha256-large");
    }

    private Task<StepResult> Point(int? context = 16384) =>
        new PointUses(_ollama, new WorkflowJob("ollama", "deploy", false, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(new ServerPlan(ComponentName.From("studio"), [new ServedModel(Interactive, Large, context is { } tokens ? ContextLength.From(tokens) : null)], [Interactive, Embedding]), TestContext.Current.CancellationToken);

    private void Holds(OllamaModel model, string weights, int? context = null) =>
        _ollama.Describe(model, Arg.Any<CancellationToken>()).Returns(new OllamaModelfile([weights],
            context is { } tokens ? new Dictionary<string, string> { ["num_ctx"] = $"{tokens}" } : new Dictionary<string, string>()));

    [Fact]
    public async Task Run_MakesAUseTheNodeDoesNotHaveYet()
    {
        await Point();

        await _ollama.Received(1).Create(Interactive, Large, ContextLength.From(16384), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_LeavesAUseAlreadyBuiltFromItsModel_WithItsContext()
    {
        Holds(Interactive, "sha256-large", 16384);

        await Point();

        _ollama.ReceivedCalls().ShouldNotContain(call => call.GetMethodInfo().Name == nameof(IOllama.Create));
    }

    [Fact]
    public async Task Run_RemakesAUseWithAnotherContext()
    {
        Holds(Interactive, "sha256-large", 8192);

        await Point();

        await _ollama.Received(1).Create(Interactive, Large, ContextLength.From(16384), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_RemakesAUseBuiltFromAnotherModel()
    {
        Holds(Interactive, "sha256-small", 16384);

        await Point();

        await _ollama.Received(1).Create(Interactive, Large, ContextLength.From(16384), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_TakesAModelsOwnContextWhenTheUseGivesNone()
    {
        Holds(Large, "sha256-large", 4096);
        Holds(Interactive, "sha256-large", 4096);

        await Point(context: null);

        _ollama.ReceivedCalls().ShouldNotContain(call => call.GetMethodInfo().Name == nameof(IOllama.Create));
    }

    [Fact]
    public async Task Run_RemovesTheNameOfAUseNoLongerDeclared_ButNotAnotherDeploymentsUse()
    {
        var stale = OllamaModel.From("lab/background:latest");
        _ollama.Installed(Arg.Any<CancellationToken>()).Returns<IReadOnlyList<OllamaModel>>([Large, Interactive, Embedding, stale]);
        Holds(Interactive, "sha256-large", 16384);

        await Point();

        await _ollama.Received(1).Remove(stale, Arg.Any<CancellationToken>());
        await _ollama.DidNotReceive().Remove(Interactive, Arg.Any<CancellationToken>());
        await _ollama.DidNotReceive().Remove(Embedding, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_DoesNothingOnANodeWithoutAServer()
    {
        await new PointUses(_ollama, new WorkflowJob("ollama", "deploy", false, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(ServerPlan.None, TestContext.Current.CancellationToken);

        _ollama.ReceivedCalls().ShouldBeEmpty();
    }
}
