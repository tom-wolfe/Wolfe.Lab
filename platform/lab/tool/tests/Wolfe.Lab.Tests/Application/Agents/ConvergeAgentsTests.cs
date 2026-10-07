using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Releases;

namespace Wolfe.Lab.Tests.Application.Agents;

public class ConvergeAgentsTests
{
    private readonly IServiceSupervisor _supervisor = Substitute.For<IServiceSupervisor>();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();

    private ConvergeAgents Step(bool dryRun = false) =>
        new(_supervisor, new WorkflowJob("agent", "deploy", dryRun, AutoApprove: true), _log);

    private static readonly Installation NotInstalled = new(new PhysicalDirectory("/lab/root/ai-ollama"), null);

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

        await Step().Run(Plan(), NotInstalled, TestContext.Current.CancellationToken);

        _log.Received().Status(Arg.Is<string>(m => m.Contains("Installed and started")));
    }

    [Fact]
    public async Task Run_NeverReportsInThePastTenseOnARehearsal()
    {
        _supervisor.Converge(Arg.Any<AgentDefinition>(), Arg.Any<CancellationToken>()).Returns(AgentOutcome.Installed);

        await Step(dryRun: true).Run(Plan(), NotInstalled, TestContext.Current.CancellationToken);

        _log.DidNotReceiveWithAnyArgs().Status(default!);
    }

    [Fact]
    public async Task Run_RetiresWhatAnAgentSupersedesBeforeConvergingIt()
    {
        var plan = new AgentPlan([Plan().Agents[0] with { Supersedes = ["sh.brew.ollama"] }]);

        await Step().Run(plan, NotInstalled, TestContext.Current.CancellationToken);

        Received.InOrder(() =>
        {
            _supervisor.Retire("sh.brew.ollama", Arg.Any<CancellationToken>());
            _supervisor.Converge(Arg.Any<AgentDefinition>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Run_StampsEveryAgentWithWhenItsInstallLastChanged()
    {
        var stamp = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

        await Step().Run(Plan(), new Installation(new PhysicalDirectory("/lab/root/ai-ollama"), stamp), TestContext.Current.CancellationToken);

        await _supervisor.Received().Converge(Arg.Is<AgentDefinition>(agent => agent.InstallStamp == stamp), Arg.Any<CancellationToken>());
    }
}
