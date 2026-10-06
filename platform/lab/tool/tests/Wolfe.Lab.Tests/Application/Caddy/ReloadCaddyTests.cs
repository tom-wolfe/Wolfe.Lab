using Ritten.Docker;
using Wolfe.Lab.Application.Caddy;
using Wolfe.Lab.Infrastructure.Caddy;

namespace Wolfe.Lab.Tests.Application.Caddy;

public class ReloadCaddyTests
{
    private readonly IDocker _docker = Substitute.For<IDocker>();

    private ReloadCaddy Step(bool dryRun = false) =>
        new(new CaddyInstance("caddy", "/etc/caddy/lab/caddy/Caddyfile"), _docker, new WorkflowJob("caddy", "renew-certs", dryRun, AutoApprove: false), Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_ForcesTheReloadWhenCaddyIsRunning()
    {
        _docker.Inspect("caddy", Arg.Any<CancellationToken>()).Returns(new ContainerState("caddy:2", Running: true));

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _docker.Received().Exec(
            Arg.Is<ContainerExec>(exec => exec.Container == "caddy" && !exec.IsReadOnly
                && exec.Arguments.SequenceEqual(new[] { "caddy", "reload", "--config", "/etc/caddy/lab/caddy/Caddyfile", "--force" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_DoesNothingWhenCaddyHasNeverStarted()
    {
        // The bootstrap order: the certificate is issued before caddy's first start, and caddy
        // reads the files then.
        _docker.Inspect("caddy", Arg.Any<CancellationToken>()).Returns((ContainerState?)null);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _docker.DidNotReceive().Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_DoesNothingWhenCaddyIsStopped()
    {
        _docker.Inspect("caddy", Arg.Any<CancellationToken>()).Returns(new ContainerState("caddy:2", Running: false));

        await Step().Run(TestContext.Current.CancellationToken);

        await _docker.DidNotReceive().Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_RehearsalReportsTheReloadWithoutMakingIt()
    {
        _docker.Inspect("caddy", Arg.Any<CancellationToken>()).Returns(new ContainerState("caddy:2", Running: true));

        await Step(dryRun: true).Run(TestContext.Current.CancellationToken);

        await _docker.DidNotReceive().Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>());
    }
}
