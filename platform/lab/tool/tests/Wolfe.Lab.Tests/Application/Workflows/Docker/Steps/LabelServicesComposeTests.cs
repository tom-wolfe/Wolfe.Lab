using System.Text.Json;
using Wolfe.Lab.Application.Workflows.Docker.Steps;
using Wolfe.Lab.Domain.Catalog;
using Component = Wolfe.Lab.Domain.Components.Component;

namespace Wolfe.Lab.Tests.Application.Workflows.Docker.Steps;

public class LabelServicesComposeTests : IDisposable
{
    private readonly DirectoryInfo _project = Directory.CreateTempSubdirectory("lab-compose-");

    public void Dispose() => _project.Delete(recursive: true);

    [Fact]
    public async Task Compose_MergesTheOverrideIntoTheServicesItFindsBesideIt()
    {
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, LabelServices.ComposeFile), """
                                                                                                 services:
                                                                                                   watcher:
                                                                                                     image: busybox
                                                                                                     environment:
                                                                                                       - TZ=Europe/London
                                                                                                 """, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, LabelServices.OverrideFile),
            LabelServices.Render(new Component(AreaName.From("personal"), ServiceName.From("mail"), ComponentName.From("watcher")), ["watcher"]), TestContext.Current.CancellationToken);

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
}
