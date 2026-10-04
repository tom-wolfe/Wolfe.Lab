using Wolfe.Lab.Domain.Telemetry;

namespace Wolfe.Lab.Tests.Domain.Telemetry;

public class TelemetryAttributeTests
{
    [Fact]
    public void Named_FindsAnAttributeByOpenTelemetrysSpelling() =>
        TelemetryAttribute.Named("lab.area").Value.ShouldBe(TelemetryAttribute.Area);

    [Fact]
    public void Labelled_FindsAnAttributeByAStoresSpelling() =>
        TelemetryAttribute.Labelled("lab_area").Value.ShouldBe(TelemetryAttribute.Area);

    [Fact]
    public void Named_SaysWhichAttributesTheLabKnows() =>
        TelemetryAttribute.Named("lab.areas").Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message
            .ShouldBe("'lab.areas' is not an attribute the lab knows (service.name, host.name, lab.area, lab.service, lab.component, lab.role).");

    [Fact]
    public void Labelled_SaysWhichLabelsTheLabKnows() =>
        TelemetryAttribute.Labelled("lab.area").Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message
            .ShouldContain("(service_name, host_name, lab_area, lab_service, lab_component, lab_role)");

    [Fact]
    public void Exactly_HoldsAListToEveryExpectedAttributeAndNothingElse()
    {
        TelemetryAttributes.Exactly("the list", ["lab.area", "lab.role"], [TelemetryAttribute.Area, TelemetryAttribute.Role]).IsSuccess.ShouldBeTrue();

        TelemetryAttributes.Exactly("the list", ["lab.area", "lab.rol"], [TelemetryAttribute.Area, TelemetryAttribute.Role])
            .Errors.ShouldNotBeNull().Select(error => error.Message).ShouldBe([
                "the list leaves out lab.role.",
                "the list: 'lab.rol' is not an attribute the lab knows (service.name, host.name, lab.area, lab.service, lab.component, lab.role)."
            ]);
    }
}
