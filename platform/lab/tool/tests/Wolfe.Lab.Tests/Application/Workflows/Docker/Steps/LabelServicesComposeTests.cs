using System.Text.Json;
using Wolfe.Lab.Application.Workflows.Docker.Steps;
using Wolfe.Lab.Infrastructure.Compose;
using Wolfe.Lab.Infrastructure.Telemetry;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Docker.Steps;

public class LabelServicesComposeTests : IDisposable
{
    private readonly DirectoryInfo _project = Directory.CreateTempSubdirectory("lab-compose-");

    public void Dispose() => _project.Delete(recursive: true);

    // The one component at <directory>, bound to the compose service <service>.
    private static ComposeBindings Bound(string directory, string service, Dictionary<ContainerLabel, string>? labels = null, string[]? publishes = null) =>
        new([
            new ComposeBinding(Catalogs.Component(directory, service), new ComposeService(service, new Dictionary<string, string>(),
                new Dictionary<string, string?>(), []), labels ?? [], publishes ?? [])
        ]);

    [Fact]
    public async Task Compose_MergesTheOverrideIntoTheServicesItFindsBesideIt()
    {
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, "compose.yaml"), """
                                                                                                 services:
                                                                                                   watcher:
                                                                                                     image: busybox
                                                                                                     environment:
                                                                                                       - TZ=Europe/London
                                                                                                 """, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, LabelServices.OverrideFile),
            LabelServices.Render(Bound("personal/mail/watcher", "watcher")), TestContext.Current.CancellationToken);

        var result = await new ProcessCommandRunner().Run(
            Command.Create("docker").WithArguments("compose", "--project-directory", _project.FullName, "config", "--format", "json").ThrowOnError(),
            TestContext.Current.CancellationToken);

        using var config = JsonDocument.Parse(result.StandardOutput);
        var watcher = config.RootElement.GetProperty("services").GetProperty("watcher");
        watcher.GetProperty("labels").GetProperty("lab.area").GetString().ShouldBe("personal");
        watcher.GetProperty("labels").GetProperty("lab.component").GetString().ShouldBe("watcher");
        var environment = watcher.GetProperty("environment");
        environment.GetProperty("TZ").GetString().ShouldBe("Europe/London");
        environment.GetProperty("OTEL_RESOURCE_ATTRIBUTES").GetString().ShouldBe("lab.area=personal,lab.service=mail,lab.component=watcher");
    }

    [Fact]
    public async Task Compose_AddsTheOverridesPublishToTheFilesOwnPorts()
    {
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, "compose.yaml"), """
                                                                                                 services:
                                                                                                   server:
                                                                                                     image: busybox
                                                                                                     ports: ["2283:2283"]
                                                                                                 """, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, LabelServices.OverrideFile),
            LabelServices.Render(Bound("personal/immich/server", "server", new() { [ContainerLabel.MetricsPort] = "8081" }, ["127.0.0.1:8081:8081"])),
            TestContext.Current.CancellationToken);

        var result = await new ProcessCommandRunner().Run(
            Command.Create("docker").WithArguments("compose", "--project-directory", _project.FullName, "config", "--format", "json").ThrowOnError(),
            TestContext.Current.CancellationToken);

        using var config = JsonDocument.Parse(result.StandardOutput);
        var server = config.RootElement.GetProperty("services").GetProperty("server");
        server.GetProperty("labels").GetProperty("lab.metrics.port").GetString().ShouldBe("8081");
        server.GetProperty("ports").EnumerateArray()
            .Select(port => $"{(port.TryGetProperty("host_ip", out var ip) ? ip.GetString() + ":" : "")}{port.GetProperty("published").GetString()}:{port.GetProperty("target").GetInt32()}")
            .ShouldBe(["2283:2283", "127.0.0.1:8081:8081"], ignoreOrder: true);
    }
}
