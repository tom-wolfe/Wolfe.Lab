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
