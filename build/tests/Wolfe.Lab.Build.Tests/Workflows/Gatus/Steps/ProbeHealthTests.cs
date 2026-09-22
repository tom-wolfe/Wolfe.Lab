using Wolfe.Lab.Build.Clients.Gatus;
using Wolfe.Lab.Build.Values;
using Wolfe.Lab.Build.Workflows.Gatus.Models;
using Wolfe.Lab.Build.Workflows.Gatus.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Gatus.Steps;

public class ProbeHealthTests
{
    private static readonly HealthProbe Probe = new(ServiceUrl.From("http://wolfe-pi5.tailf823b8.ts.net:8280/health"));
    private readonly IGatus _gatus = Substitute.For<IGatus>();

    [Fact]
    public async Task Run_PassesWhenGatusIsUp()
    {
        _gatus.Health(Probe.Url, Arg.Any<CancellationToken>()).Returns(new GatusHealth("UP"));

        var result = await new ProbeHealth(_gatus, Probe, Substitute.For<IWorkflowLog>()).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public async Task Run_FailsNamingWhatGatusSaid()
    {
        _gatus.Health(Probe.Url, Arg.Any<CancellationToken>()).Returns(new GatusHealth("DOWN"));

        var result = await new ProbeHealth(_gatus, Probe, Substitute.For<IWorkflowLog>()).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("DOWN");
    }
}
