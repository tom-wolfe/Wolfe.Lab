using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Application.Workflows.Ollama.Steps;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Infrastructure.Ollama;

namespace Wolfe.Lab.Tests.Application.Workflows.Ollama.Steps;

public class AwaitServerTests
{
    private readonly IOllama _ollama = Substitute.For<IOllama>();

    private static readonly ServerPlan Serving = new(ComponentName.From("mini"), [], []);

    private AwaitServer Step() => new(_ollama, Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_WaitsForNothingOnANodeWithoutAServer()
    {
        (await Step().Run(ServerPlan.None, TestContext.Current.CancellationToken)).IsFailure.ShouldBeFalse();

        _ollama.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_PassesAServerThatAnswers()
    {
        _ollama.AwaitServing(Arg.Any<CancellationToken>()).Returns(true);

        (await Step().Run(Serving, TestContext.Current.CancellationToken)).IsFailure.ShouldBeFalse();
    }

    [Fact]
    public async Task Run_FailsAServerThatNeverAnswers()
    {
        _ollama.AwaitServing(Arg.Any<CancellationToken>()).Returns(false);

        var result = await Step().Run(Serving, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("did not answer");
    }
}
