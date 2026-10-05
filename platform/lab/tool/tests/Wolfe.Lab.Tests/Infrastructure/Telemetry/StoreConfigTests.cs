using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Tests.Infrastructure.Telemetry;

public class StoreConfigTests
{
    [Fact]
    public void Read_HoldsPrometheusToPromotingEveryAttribute()
    {
        StoreConfig.Read("otlp:\n  promote_resource_attributes: [service.name, host.name, lab.area, lab.service, lab.component, lab.role]\n")
            .IsSuccess.ShouldBeTrue();

        StoreConfig.Read("otlp:\n  promote_resource_attributes: [service.name, host.name, lab.area, lab.service, lab.component, lab.rol]\n")
            .Errors.ShouldNotBeNull().Select(error => error.Message).ShouldBe([
                "Prometheus's otlp.promote_resource_attributes leaves out lab.role.",
                "Prometheus's otlp.promote_resource_attributes: 'lab.rol' is not an attribute the lab knows (service.name, host.name, lab.area, lab.service, lab.component, lab.role)."
            ]);
    }

    [Fact]
    public void Read_HoldsLokiToIndexingEveryAttributeButTheOneItIndexesAnyway()
    {
        StoreConfig.Read("""
            limits_config:
              otlp_config:
                resource_attributes:
                  attributes_config:
                    - action: index_label
                      attributes: [host.name, lab.role, lab.area, lab.service, lab.component]
            """).IsSuccess.ShouldBeTrue();

        StoreConfig.Read("limits_config:\n  retention_period: 168h\n").Errors.ShouldNotBeNull().Count.ShouldBe(5);
    }

    [Fact]
    public void Read_HoldsGrafanasSpanToLogsLinkToTheAttributesOwnLabel()
    {
        StoreConfig.Read(Tag("service.name", "service_name")).IsSuccess.ShouldBeTrue();
        StoreConfig.Read(Tag("service.name", "service")).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("Loki spells it service_name");
        StoreConfig.Read(Tag("service.nam", "service_nam")).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("service.nam");
        return;

        static string Tag(string key, string value) => $$"""
                                                         datasources:
                                                           - name: Tempo
                                                             jsonData:
                                                               tracesToLogsV2:
                                                                 tags:
                                                                   - { key: {{key}}, value: {{value}} }
                                                         """;
    }

    [Theory]
    [InlineData("key: [unclosed")]
    [InlineData("- a list, not a mapping")]
    [InlineData("endpoints:\n  - name: no store here\n")]
    public void Read_FindsNothingToHoldInAFileThatIsNoStoresConfiguration(string yaml)
    {
        var config = StoreConfig.Read(yaml).Value.ShouldNotBeNull();

        config.Otlp.ShouldBeNull();
        config.Limits.ShouldBeNull();
        config.Datasources.ShouldBeNull();
    }
}
