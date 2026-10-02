using Wolfe.Lab.Domain.Telemetry;
using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Tests.Infrastructure.Telemetry;

public class AlloyConfigTests
{
    [Fact]
    public void Read_HoldsTheNamesTheLabKnows()
    {
        var config = AlloyConfig.Read("""
            statements = [ "set(attributes[\"host.name\"], \"mini\") where attributes[\"lab.role\"] == nil" ]
            rule { source_labels = ["__meta_docker_container_label_lab_logs"] }
            rule { regex = "__meta_docker_container_label_(lab_.+)" }
            rule { target_label = "service_name" }
            rule { target_label = "__path__" }
            """).Value.ShouldNotBeNull();

        config.Attributes.ShouldBe([TelemetryAttribute.HostName, TelemetryAttribute.Role, TelemetryAttribute.ServiceName], ignoreOrder: true);
        config.ContainerLabels.ShouldBe([ContainerLabel.Logs]);
    }

    [Theory]
    [InlineData("""set(attributes[\"host.nme\"], \"mini\")""", "host.nme")]
    [InlineData("""rule { target_label = "lab_componet" }""", "lab_componet")]
    [InlineData("""rule { source_labels = ["__meta_docker_container_label_lab_log"] }""", "lab_log")]
    public void Read_RefusesANameTheLabDoesNotKnowOnce(string text, string name) =>
        AlloyConfig.Read(text).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain(name);
}
