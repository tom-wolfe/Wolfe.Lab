using Wolfe.Lab.Build.Steps;

namespace Wolfe.Lab.Build.Tests.Steps;

public class ApprovalGateTests
{
    private readonly IWorkflowPrompt _prompt = Substitute.For<IWorkflowPrompt>();

    private ApprovalGate Gate(bool dryRun = false, bool autoApprove = false) =>
        new(new WorkflowJob("obsidian", "sync", dryRun, autoApprove), Substitute.For<IWorkflowLog>(), _prompt);

    [Fact]
    public async Task Run_PassesWhenApprovedUpFront()
    {
        _prompt.IsInteractive.Returns(false);

        var result = await Gate(autoApprove: true).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _prompt.DidNotReceiveWithAnyArgs().Confirm(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_PassesWithoutAskingOnADryRun()
    {
        _prompt.IsInteractive.Returns(true);

        var result = await Gate(dryRun: true).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _prompt.DidNotReceiveWithAnyArgs().Confirm(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_RefusesRatherThanHangWhereNobodyCanAnswer()
    {
        _prompt.IsInteractive.Returns(false);

        var result = await Gate().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("--auto-approve");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Run_TakesTheAnswerAtATerminal(bool answer, bool fails)
    {
        _prompt.IsInteractive.Returns(true);
        _prompt.Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(answer);

        var result = await Gate().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBe(fails);
    }
}
