using Ritten.Engine.FileSystem;

namespace Wolfe.Lab.Tests.Infrastructure.Compose;

public class ComposeConfigTests : IDisposable
{
    private readonly DirectoryInfo _project = Directory.CreateTempSubdirectory("lab-compose-");

    public void Dispose() => _project.Delete(recursive: true);

    [Fact]
    public async Task Read_ReadsListsAndMapsAlike()
    {
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, "compose.yaml"), """
                                                                                               services:
                                                                                                 listed:
                                                                                                   image: busybox
                                                                                                   labels: ["lab.logs=otlp"]
                                                                                                   environment: ["TZ=Europe/London", "TOKEN"]
                                                                                                 mapped:
                                                                                                   image: busybox
                                                                                                   labels: { lab.logs: otlp }
                                                                                                   environment: { TZ: Europe/London, TOKEN: }
                                                                                               """, TestContext.Current.CancellationToken);

        var project = (await RealClients.Docker.ComposeConfig(new PhysicalDirectory(_project.FullName), ct: TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

        foreach (var service in project.Services)
        {
            service.Labels.ShouldBe(new Dictionary<string, string> { ["lab.logs"] = "otlp" });
            service.Environment["TZ"].ShouldBe("Europe/London");
            service.Environment["TOKEN"].ShouldBeNull();
        }
    }

    [Fact]
    public async Task Read_SaysWhyComposeCannotReadTheFile()
    {
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, "compose.yaml"), "services: [", TestContext.Current.CancellationToken);

        var errors = (await RealClients.Docker.ComposeConfig(new PhysicalDirectory(_project.FullName), ct: TestContext.Current.CancellationToken)).Errors.ShouldNotBeNull();

        errors.ShouldHaveSingleItem().Message.ShouldContain("yaml");
    }

    // The stack's files are compose's to find, as compose up finds them: any of its default
    // names, and an override beside it, merged — so what is read is what runs.
    [Fact]
    public async Task Read_FindsTheStackAsComposeDoes_OverrideMerged()
    {
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, "docker-compose.yml"), """
            services:
              server: { image: busybox }
            """, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, "docker-compose.override.yml"), """
            services:
              server: { labels: { lab.logs: otlp } }
            """, TestContext.Current.CancellationToken);

        var project = (await RealClients.Docker.ComposeConfig(new PhysicalDirectory(_project.FullName), ct: TestContext.Current.CancellationToken))
            .Value.ShouldNotBeNull();

        project.Services.ShouldHaveSingleItem().Labels.ShouldBe(new Dictionary<string, string> { ["lab.logs"] = "otlp" });
    }
}
