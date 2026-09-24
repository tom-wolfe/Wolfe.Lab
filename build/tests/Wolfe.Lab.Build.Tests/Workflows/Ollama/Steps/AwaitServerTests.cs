using Wolfe.Lab.Build.Clients.Ollama;
using Wolfe.Lab.Build.Workflows.Ollama.Models;
using Wolfe.Lab.Build.Workflows.Ollama.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Ollama.Steps;

public class AwaitServerTests
{
    private readonly IOllama _ollama = Substitute.For<IOllama>();

    private AwaitServer Step(bool dryRun = false, double timeoutSeconds = 5) =>
        new(_ollama, new ServerWait(TimeSpan.FromSeconds(timeoutSeconds), TimeSpan.FromMilliseconds(10)),
            new WorkflowJob("ollama", "deploy", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_PassesAServerThatIsAlreadyAnswering()
    {
        _ollama.IsServing(Arg.Any<CancellationToken>()).Returns(true);

        (await Step().Run(TestContext.Current.CancellationToken)).IsFailure.ShouldBeFalse();
    }

    [Fact]
    public async Task Run_WaitsForAServerThatIsStillStarting()
    {
        _ollama.IsServing(Arg.Any<CancellationToken>()).Returns(false, false, true);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _ollama.Received(3).IsServing(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_FailsAServerThatNeverAnswers()
    {
        _ollama.IsServing(Arg.Any<CancellationToken>()).Returns(false);

        var result = await Step(timeoutSeconds: 0.05).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("did not answer");
    }

    [Fact]
    public async Task Run_AsksNothingOnADryRun()
    {
        (await Step(dryRun: true).Run(TestContext.Current.CancellationToken)).IsFailure.ShouldBeFalse();

        await _ollama.DidNotReceiveWithAnyArgs().IsServing(TestContext.Current.CancellationToken);
    }
}
