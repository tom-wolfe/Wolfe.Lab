using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Domain.Catalog.Facets.Telemetry;
using Wolfe.Lab.Domain.Network;
using Wolfe.Lab.Infrastructure.Compose;
using Wolfe.Lab.Infrastructure.Telemetry;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Infrastructure.Compose;

public class ComposeBindingsTests
{
    private const string Immich = "personal/immich/compose";

    private static ComposeService Service(string name, ComposePort[]? ports = null, string? networkMode = null, Dictionary<string, string>? labels = null) =>
        new(name, labels ?? [], new Dictionary<string, string?>(), ports ?? [], networkMode);

    private static Result<ComposeBindings> Bind(ComposeService[] services, params Catalogs.Declaration[] components) =>
        ComposeBindings.Of(Catalogs.Unit(Immich, components), new ComposeProject(services));

    private static IReadOnlyList<string> Errors(ComposeService[] services, params Catalogs.Declaration[] components) =>
        [.. Bind(services, components).Errors.ShouldNotBeNull().Select(error => error.Message)];

    private static MetricsEndpoint Metrics(int port) => new(Port.From(port), HttpPath.Metrics);

    [Fact]
    public void Of_BindsEachServiceToTheComponentThatNamesIt()
    {
        var bindings = Bind([Service("immich-server"), Service("immich-database")],
            Catalogs.Compose("server", "immich-server"),
            Catalogs.Compose("postgres", "immich-database", ComponentKind.Database),
            Catalogs.Definition("database", ComponentKind.Database, WorkflowName.Restic, partOf: "postgres")).Value.ShouldNotBeNull();

        bindings.Bindings.Select(binding => (binding.Service.Name, binding.Component.Name.Value))
            .ShouldBe([("immich-database", "postgres"), ("immich-server", "server")]);
        bindings.Bindings.ShouldAllBe(binding => binding.Labels.Count == 0 && binding.Publishes.Count == 0);
    }

    [Fact]
    public void Of_RefusesAServiceNoComponentDeclares() =>
        Errors([Service("immich-server"), Service("immich-redis")], Catalogs.Compose("server", "immich-server"))
            .ShouldBe(["the compose stack's service 'immich-redis' is no component's: declare one in personal/immich/compose, with service: immich-redis."]);

    [Fact]
    public void Of_RefusesAComponentNamingAServiceTheStackHasNot_AtItsField() =>
        Bind([Service("immich-server")], Catalogs.Compose("server", "immich-server"), Catalogs.Compose("other", "immich-sever"))
            .Errors.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeOfType<CatalogError>()
            .ShouldSatisfyAllConditions(
                error => error.Field.ShouldBe("service"),
                error => error.Problem.ShouldBe(ComposeBindingErrors.NotInTheStack(ComposeServiceName.From("immich-sever"), ["immich-server"])),
                error => error.Source.Index.ShouldBe(1));

    [Fact]
    public void Of_RefusesTwoComponentsOfOneService() =>
        Errors([Service("immich-server")], Catalogs.Compose("server", "immich-server"), Catalogs.Compose("web", "immich-server"))
            .ShouldHaveSingleItem().ShouldContain("'immich-server' is component 'server''s already");

    [Fact]
    public void Of_LabelsAComponentThatSendsItsOwnLogs()
    {
        var binding = Bind([Service("watcher")], Catalogs.Compose("watcher", "watcher", logs: LogTransport.Otlp)).Value.ShouldNotBeNull().Bindings.ShouldHaveSingleItem();

        binding.Labels.ShouldBe(new Dictionary<ContainerLabel, string> { [ContainerLabel.Logs] = "otlp" });
    }

    [Fact]
    public void Of_PublishesAMetricsPortOnLoopbackWhenTheFileDoesNot()
    {
        var binding = Bind([Service("immich-server", [new(2283, 2283, null, "tcp")])], Catalogs.Compose("server", "immich-server", metrics: Metrics(8081)))
            .Value.ShouldNotBeNull().Bindings.ShouldHaveSingleItem();

        binding.Publishes.ShouldBe(["127.0.0.1:8081:8081"]);
        binding.Metrics.ShouldBe([new MetricsTarget(Port.From(8081), HttpPath.Metrics)]);
        binding.Labels.ShouldBeEmpty();
    }

    [Fact]
    public void Of_ScrapesAPortTheFilePublishesAlreadyWhereItIsPublished()
    {
        var binding = Bind([Service("garage", [new(3903, 13903, null, "tcp")])], Catalogs.Compose("garage", "garage", metrics: Metrics(3903)))
            .Value.ShouldNotBeNull().Bindings.ShouldHaveSingleItem();

        binding.Publishes.ShouldBeEmpty();
        binding.Metrics.ShouldBe([new MetricsTarget(Port.From(13903), HttpPath.Metrics)]);
    }

    [Fact]
    public void Of_ScrapesAServiceOnTheHostsNetworkOnItsOwnPort() =>
        Bind([Service("agent", networkMode: "host")], Catalogs.Compose("agent", "agent", metrics: Metrics(9100)))
            .Value.ShouldNotBeNull().Bindings.ShouldHaveSingleItem().Metrics.ShouldBe([new MetricsTarget(Port.From(9100), HttpPath.Metrics)]);

    [Fact]
    public void Of_PublishesAMetricsPortWhereTheComponentChooses()
    {
        var binding = Bind([Service("grafana", [new(3000, 3000, null, "tcp")])],
                Catalogs.Compose("grafana", "grafana", metrics: Metrics(3000) with { Published = Port.From(13000) }))
            .Value.ShouldNotBeNull().Bindings.ShouldHaveSingleItem();

        binding.Publishes.ShouldBe(["127.0.0.1:13000:3000"]);
        binding.Metrics.ShouldBe([new MetricsTarget(Port.From(13000), HttpPath.Metrics)]);
    }

    [Fact]
    public void Of_PublishesEachOfSeveralEndpoints()
    {
        var binding = Bind([Service("immich-server", [new(2283, 2283, null, "tcp")])],
                Catalogs.Compose("server", "immich-server", metrics: [Metrics(8081), Metrics(8082)]))
            .Value.ShouldNotBeNull().Bindings.ShouldHaveSingleItem();

        binding.Publishes.ShouldBe(["127.0.0.1:8081:8081", "127.0.0.1:8082:8082"]);
        binding.Metrics.Select(target => target.Port.Value).ShouldBe([8081, 8082]);
    }

    [Fact]
    public void Of_RefusesLogsDeclaredHereAndByTheirLabelToo() =>
        Errors([Service("watcher", labels: new() { ["lab.logs"] = "otlp" })], Catalogs.Compose("watcher", "watcher", logs: LogTransport.Otlp))
            .ShouldHaveSingleItem().ShouldContain("logs: the component's logs are declared twice");

    [Fact]
    public void Of_RefusesMetricsOnAServiceInAnothersNetwork() =>
        Errors([Service("server"), Service("sidecar", networkMode: "service:server")],
                Catalogs.Compose("server", "server"), Catalogs.Compose("sidecar", "sidecar", metrics: Metrics(9090)))
            .ShouldHaveSingleItem().ShouldContain("metrics: its service runs in service:server's network");
}
