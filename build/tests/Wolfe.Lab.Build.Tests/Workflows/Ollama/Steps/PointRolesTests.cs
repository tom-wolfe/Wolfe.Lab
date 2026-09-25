using Wolfe.Lab.Build.Clients.Ollama;
using Wolfe.Lab.Build.Workflows.Ollama.Models;
using Wolfe.Lab.Build.Workflows.Ollama.Steps;
using OllamaModel = Wolfe.Lab.Build.Clients.Ollama.OllamaModel;

namespace Wolfe.Lab.Build.Tests.Workflows.Ollama.Steps;

public class PointRolesTests
{
    private static readonly OllamaModel Background = OllamaModel.From("lab/background:latest");
    private static readonly OllamaModel Small = OllamaModel.From("qwen3:8b");
    private static readonly OllamaModel Large = OllamaModel.From("qwen3:30b-a3b");

    private readonly IOllama _ollama = Substitute.For<IOllama>();

    private PointRoles Step(params (OllamaModel Alias, OllamaModel Model)[] roles) =>
        new(new RolePlan(roles.ToDictionary(r => r.Alias, r => r.Model)), _ollama,
            new WorkflowJob("ollama", "deploy", false, AutoApprove: true), Substitute.For<IWorkflowLog>());

    private void Listed(params (OllamaModel Model, string Id)[] models) =>
        _ollama.Identities(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyDictionary<OllamaModel, string>>(models.ToDictionary(m => m.Model, m => m.Id));

    [Fact]
    public async Task Run_PointsARoleTheNodeDoesNotHaveYet()
    {
        Listed((Large, "aaa"));

        await Step((Background, Large)).Run(TestContext.Current.CancellationToken);

        await _ollama.Received(1).Copy(Large, Background, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_LeavesARoleThatAlreadyPointsThere()
    {
        Listed((Large, "aaa"), (Background, "aaa"));

        await Step((Background, Large)).Run(TestContext.Current.CancellationToken);

        _ollama.ReceivedCalls().ShouldNotContain(call => call.GetMethodInfo().Name == nameof(IOllama.Copy));
    }

    [Fact]
    public async Task Run_RepointsARoleThatPointsElsewhere()
    {
        Listed((Small, "bbb"), (Large, "aaa"), (Background, "bbb"));

        await Step((Background, Large)).Run(TestContext.Current.CancellationToken);

        await _ollama.Received(1).Copy(Large, Background, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_RemovesTheAliasOfARoleNoLongerDeclared()
    {
        Listed((Small, "bbb"), (Background, "bbb"));

        await Step().Run(TestContext.Current.CancellationToken);

        await _ollama.Received(1).Remove(Background, Arg.Any<CancellationToken>());
        await _ollama.DidNotReceive().Remove(Small, Arg.Any<CancellationToken>());
    }
}
