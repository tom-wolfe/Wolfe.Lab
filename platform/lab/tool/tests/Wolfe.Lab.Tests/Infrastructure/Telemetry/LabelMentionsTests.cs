using Wolfe.Lab.Domain.Telemetry;
using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Tests.Infrastructure.Telemetry;

public class LabelMentionsTests
{
    [Fact]
    public void In_HoldsTheLabelsAQueryNames()
    {
        var mentions = LabelMentions.In("expr: sum by (lab_area, lab_role) (up{lab_logs!=\"otlp\"})").Value.ShouldNotBeNull();

        mentions.Attributes.ShouldBe([TelemetryAttribute.Area, TelemetryAttribute.Role]);
        mentions.ContainerLabels.ShouldBe([ContainerLabel.Logs]);
    }

    [Fact]
    public void In_RefusesAnUnknownLabelInAQuery() =>
        LabelMentions.In("expr: sum by (lab_areas) (up)").Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("lab_areas");

    [Fact]
    public void In_IgnoresALabelThatOnlyEndsInTheLabsPrefix() =>
        LabelMentions.In("expr: sum by (my_lab_thing) (up)").IsSuccess.ShouldBeTrue();
}
