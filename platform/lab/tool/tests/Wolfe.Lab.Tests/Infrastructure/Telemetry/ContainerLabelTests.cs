using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Tests.Infrastructure.Telemetry;

public class ContainerLabelTests
{
    [Fact]
    public void Understands_OnlyAValueTheCollectorDoes()
    {
        ContainerLabel.Logs.Understands("otlp").ShouldBeTrue();
        ContainerLabel.Logs.Understands("stdout").ShouldBeFalse();
    }

    [Fact]
    public void Understands_AnyValueOfALabelWithNoList() =>
        ContainerLabel.MetricsPort.Understands("8081").ShouldBeTrue();

    [Fact]
    public void Named_RefusesALabelTheLabDoesNotKnow_AsTheWellKnownError() =>
        ContainerLabel.Named("lab.log").Errors.ShouldNotBeNull().ShouldHaveSingleItem()
            .ShouldBe(ContainerLabelErrors.Unknown("lab.log", ContainerLabel.All.Select(label => label.Name)));
}
