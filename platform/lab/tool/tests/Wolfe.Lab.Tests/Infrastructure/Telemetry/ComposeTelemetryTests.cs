using Ritten.Docker;
using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Tests.Infrastructure.Telemetry;

public class ComposeTelemetryTests
{
    private static ComposeProject Project(Dictionary<string, string>? labels = null, Dictionary<string, string?>? environment = null) =>
        new([new ComposeService("watcher", labels ?? [], environment ?? [], [])]);

    [Fact]
    public void From_HoldsTheCollectorsLabelsAndLeavesOthersAlone()
    {
        var telemetry = ComposeTelemetry.From(Project(
            new Dictionary<string, string> { ["lab.logs"] = "otlp", ["com.example.other"] = "anything" },
            new Dictionary<string, string?> { ["TZ"] = "Europe/London" })).Value.ShouldNotBeNull();

        telemetry.Labels["watcher"].ShouldBe(new Dictionary<ContainerLabel, string> { [ContainerLabel.Logs] = "otlp" });
    }

    [Fact]
    public void From_LeavesOutAServiceThatSetsNothingForTheCollector() =>
        ComposeTelemetry.From(Project()).Value.ShouldNotBeNull().Labels.ShouldBeEmpty();

    [Theory]
    [InlineData("lab.area", "personal", "the deploy sets")]
    [InlineData("lab.component", "watcher", "the deploy sets")]
    [InlineData("lab.logs", "stdout", "the collector understands otlp")]
    [InlineData("lab.log", "otlp", "not a label the lab knows")]
    [InlineData("lab.metrics.port", "8081", "the deploy sets from the component's metrics")]
    [InlineData("lab.metrics.path", "/metrics", "the deploy sets from the component's metrics")]
    public void From_RefusesALabelTheDeployOwnsOrTheCollectorWouldNotUnderstand(string label, string value, string expected) =>
        ComposeTelemetry.From(Project(new Dictionary<string, string> { [label] = value })).Errors.ShouldNotBeNull()
            .ShouldHaveSingleItem().Message.ShouldContain(expected);

    [Fact]
    public void From_RefusesTheResourceAttributesTheDeploySets() =>
        ComposeTelemetry.From(Project(environment: new Dictionary<string, string?> { ["OTEL_RESOURCE_ATTRIBUTES"] = "lab.area=personal" })).Errors.ShouldNotBeNull()
            .ShouldHaveSingleItem().Message.ShouldContain("OTEL_RESOURCE_ATTRIBUTES");

    [Fact]
    public void From_ReportsEveryProblemAtOnce() =>
        ComposeTelemetry.From(Project(
            new Dictionary<string, string> { ["lab.area"] = "personal", ["lab.logs"] = "stdout" },
            new Dictionary<string, string?> { ["OTEL_RESOURCE_ATTRIBUTES"] = null })).Errors.ShouldNotBeNull().Count.ShouldBe(3);
}
