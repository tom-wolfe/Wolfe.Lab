using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Tests.Infrastructure.Telemetry;

public class TelemetryNamesTests
{
    [Fact]
    public void Compose_AcceptsTheLabelsMeantForIt() =>
        TelemetryNames.Compose("""
            services:
              watcher:
                labels:
                  lab.logs: otlp
                  com.example.other: anything
                environment:
                  TZ: Europe/London
            """).ShouldBeEmpty();

    [Theory]
    [InlineData("labels:\n      lab.area: personal", "the deploy sets")]
    [InlineData("labels:\n      - lab.component=watcher", "the deploy sets")]
    [InlineData("labels:\n      lab.logs: stdout", "the collector understands otlp")]
    [InlineData("labels:\n      lab.log: otlp", "not one the lab knows")]
    [InlineData("environment:\n      OTEL_RESOURCE_ATTRIBUTES: lab.area=personal", "OTEL_RESOURCE_ATTRIBUTES")]
    [InlineData("environment:\n      - OTEL_RESOURCE_ATTRIBUTES=lab.area=personal", "OTEL_RESOURCE_ATTRIBUTES")]
    public void Compose_RefusesWhatTheDeployOwnsOrTheCollectorWouldNotUnderstand(string definition, string expected) =>
        TelemetryNames.Compose($"services:\n  watcher:\n    {definition}\n").ShouldHaveSingleItem().ShouldContain(expected);

    [Fact]
    public void Alloy_AcceptsTheNamesTheLabKnows() =>
        TelemetryNames.Alloy("""
            statements = [ "set(attributes[\"host.name\"], \"mini\") where attributes[\"lab.role\"] == nil" ]
            rule { source_labels = ["__meta_docker_container_label_lab_logs"] }
            rule { regex = "__meta_docker_container_label_(lab_.+)" }
            rule { target_label = "service_name" }
            rule { target_label = "__path__" }
            """).ShouldBeEmpty();

    [Theory]
    [InlineData("""set(attributes[\"host.nme\"], \"mini\")""", "host.nme")]
    [InlineData("""rule { target_label = "lab_componet" }""", "lab_componet")]
    [InlineData("""rule { source_labels = ["__meta_docker_container_label_lab_log"] }""", "lab_log")]
    public void Alloy_RefusesANameTheLabDoesNotKnow(string text, string name) =>
        TelemetryNames.Alloy(text).ShouldContain(problem => problem.Contains(name));

    [Fact]
    public void Yaml_RefusesAnUnknownLabelInAQuery() =>
        TelemetryNames.Yaml("expr: sum by (lab_areas) (up)").ShouldHaveSingleItem().ShouldContain("lab_areas");

    [Fact]
    public void Yaml_HoldsPrometheusToPromotingEveryAttribute()
    {
        const string all = "[service.name, host.name, lab.area, lab.service, lab.component, lab.role]";
        TelemetryNames.Yaml($"otlp:\n  promote_resource_attributes: {all}\n").ShouldBeEmpty();

        TelemetryNames.Yaml("otlp:\n  promote_resource_attributes: [service.name, host.name, lab.area, lab.service, lab.component, lab.rol]\n")
            .ShouldBe(["Prometheus's otlp.promote_resource_attributes leaves out lab.role.",
                "Prometheus's otlp.promote_resource_attributes names lab.rol, which is not an attribute the lab knows (service.name, host.name, lab.area, lab.service, lab.component, lab.role)."]);
    }

    [Fact]
    public void Yaml_HoldsLokiToIndexingEveryAttributeButTheOneItIndexesAnyway()
    {
        TelemetryNames.Yaml("""
            limits_config:
              otlp_config:
                resource_attributes:
                  attributes_config:
                    - action: index_label
                      attributes: [host.name, lab.role, lab.area, lab.service, lab.component]
            """).ShouldBeEmpty();

        TelemetryNames.Yaml("limits_config:\n  retention_period: 168h\n").Count().ShouldBe(5);
    }

    [Fact]
    public void Yaml_HoldsGrafanasSpanToLogsLinkToTheAttributesOwnLabel()
    {
        static string Tag(string key, string value) => $$"""
            datasources:
              - name: Tempo
                jsonData:
                  tracesToLogsV2:
                    tags:
                      - { key: {{key}}, value: {{value}} }
            """;

        TelemetryNames.Yaml(Tag("service.name", "service_name")).ShouldBeEmpty();
        TelemetryNames.Yaml(Tag("service.name", "service")).ShouldHaveSingleItem().ShouldContain("Loki spells it service_name");
        TelemetryNames.Yaml(Tag("service.nam", "service_nam")).ShouldHaveSingleItem().ShouldContain("service.nam");
    }

    [Fact]
    public void Yaml_LeavesAFileThatIsNotYamlToItsOwnTool() =>
        TelemetryNames.Yaml("key: [unclosed").ShouldBeEmpty();

    [Fact]
    public void Targets_AcceptTheLabsLabelsAndTheCollectorsOwn() =>
        TelemetryNames.Targets("""
            [{ "targets": ["localhost"], "labels": { "__path__": "/tmp/x.log", "service_name": "x", "lab_area": "platform", "lab_component": "runners" } }]
            """).ShouldBeEmpty();

    [Fact]
    public void Targets_RefuseALabelTheLabDoesNotKnow() =>
        TelemetryNames.Targets("""[{ "targets": ["localhost"], "labels": { "__path__": "/tmp/x.log", "lab_componet": "runners" } }]""")
            .ShouldHaveSingleItem().ShouldContain("lab_componet");

    [Fact]
    public void Targets_LeaveOtherJsonAlone()
    {
        TelemetryNames.Targets("""{ "workflow": "docker", "release": "caddy" }""").ShouldBeEmpty();
        TelemetryNames.Targets("{{ .template }}").ShouldBeEmpty();
    }
}
