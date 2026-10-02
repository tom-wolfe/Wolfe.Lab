using Wolfe.Lab.Infrastructure.Compose;

namespace Wolfe.Lab.Tests.Infrastructure.Compose;

public class ComposeProjectTests
{
    [Fact]
    public void Parse_ReadsEachServicesLabelsAndEnvironment()
    {
        var project = ComposeProject.Parse("""
            { "services": {
                "watcher": { "labels": { "lab.logs": "otlp" }, "environment": { "TZ": "Europe/London", "TOKEN": null } },
                "bridge": { "image": "busybox" } } }
            """).Value.ShouldNotBeNull();

        project.Services.Select(service => service.Name).ShouldBe(["bridge", "watcher"]);
        project.Services[0].Labels.ShouldBeEmpty();
        project.Services[1].Labels["lab.logs"].ShouldBe("otlp");
        project.Services[1].Environment["TOKEN"].ShouldBeNull();
    }

    [Fact]
    public void Parse_RefusesWhatIsNotComposesConfiguration() =>
        ComposeProject.Parse("not json").IsError.ShouldBeTrue();
}

// Against the real compose: that it reads both of a file's spellings as one is what the lab relies on.
public class ComposeProjectComposeTests : IDisposable
{
    private readonly DirectoryInfo _project = Directory.CreateTempSubdirectory("lab-compose-");

    public void Dispose() => _project.Delete(recursive: true);

    [Fact]
    public async Task Read_ReadsListsAndMapsAlike()
    {
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, ComposeProject.FileName), """
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

        var project = (await ComposeProject.Read(new ProcessCommandRunner(), _project.FullName, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

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
        await File.WriteAllTextAsync(Path.Combine(_project.FullName, ComposeProject.FileName), "services: [", TestContext.Current.CancellationToken);

        var errors = (await ComposeProject.Read(new ProcessCommandRunner(), _project.FullName, TestContext.Current.CancellationToken)).Errors.ShouldNotBeNull();

        errors.ShouldHaveSingleItem().Message.ShouldStartWith("compose cannot read compose.yaml");
    }
}
