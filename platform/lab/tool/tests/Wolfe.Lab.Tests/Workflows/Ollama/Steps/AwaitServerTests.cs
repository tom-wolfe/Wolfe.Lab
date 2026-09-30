using Wolfe.Lab.Clients.Ollama;
using Wolfe.Lab.Workflows.Ollama.Steps;

namespace Wolfe.Lab.Tests.Workflows.Ollama.Steps;

public class AwaitServerTests
{
    private readonly IOllama _ollama = Substitute.For<IOllama>();

    private AwaitServer Step() => new(_ollama, Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_PassesAServerThatAnswers()
    {
        _ollama.AwaitServing(Arg.Any<CancellationToken>()).Returns(true);

        (await Step().Run(TestContext.Current.CancellationToken)).IsFailure.ShouldBeFalse();
    }

    [Fact]
    public async Task Run_FailsAServerThatNeverAnswers()
    {
        _ollama.AwaitServing(Arg.Any<CancellationToken>()).Returns(false);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("did not answer");
    }
}
