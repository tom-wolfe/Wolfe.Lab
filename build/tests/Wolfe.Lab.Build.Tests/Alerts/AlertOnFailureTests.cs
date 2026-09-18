using Microsoft.Extensions.Options;
using Ritten.Engine.Runs;
using Ritten.Forgejo;
using Wolfe.Lab.Build.Alerts;

namespace Wolfe.Lab.Build.Tests.Alerts;

public class AlertOnFailureTests
{
    private readonly IAlerts _alerts = Substitute.For<IAlerts>();
    private const string RunUrl = "http://forgejo/tom-wolfe/Wolfe.Lab/actions/runs/42";
    private static readonly IOptions<ForgejoActionsOptions> Run = Options.Create(new ForgejoActionsOptions { RunUrl = RunUrl });

    private static StepOutcome Failed(string step, string error) =>
        new(Step.FromType<FailingStep>(), StepResult.Failed(new Error(error)));

    [Step("push vault", StepKind.Work)]
    private sealed class FailingStep
    {
        public StepResult Run() => StepResult.Failed(new Error("boom"));
    }

    [Fact]
    public async Task Publish_SaysNothingOnSuccess()
    {
        var sink = new AlertOnFailure(_alerts, Run);

        await sink.Publish(new WorkflowReport("obsidian", true, []), TestContext.Current.CancellationToken);

        await _alerts.DidNotReceiveWithAnyArgs().Send(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_NamesTheJobTheStepAndTheRun()
    {
        var sink = new AlertOnFailure(_alerts, Run);
        await sink.Started(new WorkflowJob("obsidian", "sync"), TestContext.Current.CancellationToken);

        await sink.Publish(new WorkflowReport("obsidian", false, [], Failed("push vault", "Could not read the token.")), TestContext.Current.CancellationToken);

        await _alerts.Received().Send(
            new Alert("obsidian sync failed", "push vault: Could not read the token.\nhttp://forgejo/tom-wolfe/Wolfe.Lab/actions/runs/42"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Message_LeavesOutWhatIsNotKnown()
    {
        AlertOnFailure.Message(new WorkflowReport("t", false, []), null).ShouldBe("");
        AlertOnFailure.Message(new WorkflowReport("t", false, []), RunUrl).ShouldBe(RunUrl);
    }
}
