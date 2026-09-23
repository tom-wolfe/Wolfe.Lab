using Ritten.Docker;
using Wolfe.Lab.Build.Clients.Caddy.Steps;

namespace Wolfe.Lab.Build.Tests.Clients.Caddy.Steps;

public class ReloadCaddyTests
{
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    private ReloadCaddy Step(bool dryRun = false) =>
        new(_docker, _commands, new WorkflowJob("caddy", "renew-certs", dryRun, AutoApprove: false), Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_ForcesTheReloadWhenCaddyIsRunning()
    {
        _docker.Inspect("caddy", Arg.Any<CancellationToken>()).Returns(new ContainerState("caddy:2", Running: true));
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "", ""));

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        var command = (Command)_commands.ReceivedCalls().Single().GetArguments()[0]!;
        command.Arguments.ShouldBe(["exec", "caddy", "caddy", "reload", "--config", "/etc/caddy/lab/caddy/Caddyfile", "--force"]);
    }

    [Fact]
    public async Task Run_DoesNothingWhenCaddyHasNeverStarted()
    {
        // The bootstrap order: the certificate is issued before caddy's first start, and caddy
        // reads the files then.
        _docker.Inspect("caddy", Arg.Any<CancellationToken>()).Returns<ContainerState>(_ => throw new CommandFailedException("No such object: caddy", new CommandResult(1, "", "No such object")));

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _commands.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_DoesNothingWhenCaddyIsStopped()
    {
        _docker.Inspect("caddy", Arg.Any<CancellationToken>()).Returns(new ContainerState("caddy:2", Running: false));

        await Step().Run(TestContext.Current.CancellationToken);

        _commands.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_RehearsalReportsTheReloadWithoutMakingIt()
    {
        _docker.Inspect("caddy", Arg.Any<CancellationToken>()).Returns(new ContainerState("caddy:2", Running: true));

        await Step(dryRun: true).Run(TestContext.Current.CancellationToken);

        _commands.ReceivedCalls().ShouldBeEmpty();
    }
}
