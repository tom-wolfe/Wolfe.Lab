using Wolfe.Lab.Build.Agents;
using Wolfe.Lab.Build.Agents.Steps;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Tests.Agents.Steps;

public class ConvergeAgentsTests
{
    private readonly IServiceSupervisor _supervisor = Substitute.For<IServiceSupervisor>();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();

    private ConvergeAgents Step(bool dryRun = false) =>
        new(_supervisor, new WorkflowJob("agents", "deploy", dryRun, AutoApprove: true), _log);

    private static AgentPlan Plan() => new([
        new AgentDefinition(
            AgentLabel.ForName("ollama").ShouldNotBeNull(),
            HostPath.From("/opt/homebrew/bin/ollama"),
            ["serve"],
            new Dictionary<string, string>(),
            null,
            null,
            true,
            null,
            AgentRestart.Reload,
            DateTimeOffset.UnixEpoch)
    ]);

    [Fact]
    public async Task Run_SaysWhatItDid()
    {
        _supervisor.Converge(Arg.Any<AgentDefinition>(), Arg.Any<CancellationToken>()).Returns(AgentOutcome.Installed);

        await Step().Run(Plan(), TestContext.Current.CancellationToken);

        _log.Received().Status(Arg.Is<string>(m => m.Contains("Installed and started")));
    }

    [Fact]
    public async Task Run_NeverReportsInThePastTenseOnARehearsal()
    {
        _supervisor.Converge(Arg.Any<AgentDefinition>(), Arg.Any<CancellationToken>()).Returns(AgentOutcome.Installed);

        await Step(dryRun: true).Run(Plan(), TestContext.Current.CancellationToken);

        _log.DidNotReceiveWithAnyArgs().Status(default!);
    }
}
