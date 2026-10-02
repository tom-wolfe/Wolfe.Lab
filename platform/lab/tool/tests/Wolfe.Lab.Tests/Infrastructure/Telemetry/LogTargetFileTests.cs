using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Tests.Infrastructure.Telemetry;

public class LogTargetFileTests
{
    [Fact]
    public void Read_HoldsTargetsLabelledWithTheLabsLabelsAndTheCollectorsOwn()
    {
        var file = LogTargetFile.Read("""
            [{ "targets": ["localhost"], "labels": { "__path__": "/tmp/x.log", "service_name": "x", "lab_area": "platform", "lab_component": "runners" } }]
            """).Value.ShouldNotBeNull();

        file.Targets.ShouldHaveSingleItem().Labels[LogTargetFile.PathLabel].ShouldBe("/tmp/x.log");
    }

    [Fact]
    public void Read_RefusesALabelTheLabDoesNotKnow() =>
        LogTargetFile.Read("""[{ "targets": ["localhost"], "labels": { "__path__": "/tmp/x.log", "lab_componet": "runners" } }]""")
            .Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("lab_componet");

    [Theory]
    [InlineData("""{ "workflow": "docker", "release": "caddy" }""")]
    [InlineData("{{ .template }}")]
    public void Read_FindsNoTargetsInOtherJson(string json) =>
        LogTargetFile.Read(json).Value.ShouldNotBeNull().Targets.ShouldBeEmpty();
}
