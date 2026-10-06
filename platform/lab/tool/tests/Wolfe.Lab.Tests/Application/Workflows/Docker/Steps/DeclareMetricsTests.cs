using Ritten.Docker;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Docker.Steps;
using Wolfe.Lab.Domain.Network;
using Wolfe.Lab.Infrastructure.Compose;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Docker.Steps;

public class DeclareMetricsTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-metrics-");

    public void Dispose() => _root.Delete(recursive: true);

    private static ComposeBindings Bound(params MetricsTarget[] metrics) =>
        new([
            new ComposeBinding(Catalogs.Component("personal/immich/compose", "server"),
                new ComposeService("immich-server", new Dictionary<string, string>(), new Dictionary<string, string?>(), []) { ContainerName = "immich-server" },
                new Dictionary<Wolfe.Lab.Infrastructure.Telemetry.ContainerLabel, string>(), []) { Metrics = metrics }
        ]);

    private Task<StepResult> Declare(ComposeBindings bindings, bool dryRun = false) =>
        new DeclareMetrics(Options.Create(new LabDirectories { Root = new PhysicalDirectory(_root.FullName) }),
                new WorkflowJob("docker", "deploy", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(bindings, TestContext.Current.CancellationToken);

    private string TargetFile => Path.Combine(_root.FullName, ".metrics", "personal-immich-server.json");

    [Fact]
    public async Task Run_NamesEachEndpointForItsContainerAndWhereItLives()
    {
        (await Declare(Bound(new MetricsTarget(Port.From(8081), HttpPath.Metrics), new MetricsTarget(Port.From(18082), HttpPath.From("/stats"))))).IsFailure.ShouldBeFalse();

        using var written = JsonDocument.Parse(await File.ReadAllTextAsync(TargetFile, TestContext.Current.CancellationToken));
        var targets = written.RootElement.EnumerateArray().ToList();
        targets.Select(target => target.GetProperty("targets")[0].GetString()).ShouldBe(["127.0.0.1:8081", "127.0.0.1:18082"]);
        var labels = targets[1].GetProperty("labels");
        labels.GetProperty("__metrics_path__").GetString().ShouldBe("/stats");
        labels.GetProperty("service_name").GetString().ShouldBe("immich-server");
        labels.GetProperty("lab_area").GetString().ShouldBe("personal");
        labels.GetProperty("lab_service").GetString().ShouldBe("immich");
        labels.GetProperty("lab_component").GetString().ShouldBe("server");
    }

    [Fact]
    public async Task Run_RetiresTheFileOfAComponentThatNoLongerDeclaresMetrics()
    {
        await Declare(Bound(new MetricsTarget(Port.From(8081), HttpPath.Metrics)));

        await Declare(Bound());

        File.Exists(TargetFile).ShouldBeFalse();
    }

    [Fact]
    public async Task Run_WritesNothingInARehearsal()
    {
        await Declare(Bound(new MetricsTarget(Port.From(8081), HttpPath.Metrics)), dryRun: true);

        File.Exists(TargetFile).ShouldBeFalse();
    }
}
