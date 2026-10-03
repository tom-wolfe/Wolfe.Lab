using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Docker.Models;
using Wolfe.Lab.Application.Workflows.Docker.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Infrastructure.Releases;
using YamlDotNet.Serialization;
using Component = Wolfe.Lab.Domain.Components.Component;

namespace Wolfe.Lab.Tests.Application.Workflows.Docker.Steps;

public class LabelServicesTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-label-");
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly DirectoryInfo _release;

    public LabelServicesTests()
    {
        var checkout = _root.CreateSubdirectory("checkout");
        var component = checkout.CreateSubdirectory("personal").CreateSubdirectory("mail").CreateSubdirectory("watcher");
        _release = _root.CreateSubdirectory("release");
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(component.FullName));
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "watcher\nbridge\n", ""));
    }

    public void Dispose() => _root.Delete(recursive: true);

    private Task<StepResult> Run(bool dryRun = false) =>
        new LabelServices(_commands, _fileSystem, new WorkflowJob("docker", "deploy", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(new Release("mail-watcher", new PhysicalDirectory(_release.FullName)), new Component(AreaName.From("personal"), ServiceName.From("mail"), ComponentName.From("watcher")), ComposeEnvironment.Empty, TestContext.Current.CancellationToken);

    private string Override => Path.Combine(_release.FullName, LabelServices.OverrideFile);

    [Fact]
    public async Task Run_LabelsEveryServiceWithWhereTheComponentSits()
    {
        (await Run()).IsFailure.ShouldBeFalse();

        var text = await File.ReadAllTextAsync(Override, TestContext.Current.CancellationToken);
        text.ShouldNotContain("&"); // written out per service, not as anchors and aliases
        var written = new DeserializerBuilder().Build()
            .Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>>(text);
        var services = written["services"];
        services.Keys.ShouldBe(["bridge", "watcher"], ignoreOrder: true);
        foreach (var service in services.Values)
        {
            service["labels"].ShouldBe(new Dictionary<string, string>
            {
                ["lab.area"] = "personal",
                ["lab.service"] = "mail",
                ["lab.component"] = "watcher"
            });
            service["environment"]["OTEL_RESOURCE_ATTRIBUTES"].ShouldBe("lab.area=personal,lab.service=mail,lab.component=watcher");
        }
    }

    [Fact]
    public async Task Run_AsksComposeForTheServicesOfTheComponentsOwnFileAlone()
    {
        await Run();

        // The file named outright: a stale override in the release must not be read as services.
        await _commands.Received().Run(
            Arg.Is<Command>(c => c.Path == "docker" && c.Arguments.Contains("-f") && c.Arguments.Contains("--services")
                && c.Arguments.Any(a => a.EndsWith(Path.Combine("watcher", LabelServices.ComposeFile), StringComparison.Ordinal))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_WritesNothingWhenRehearsing()
    {
        (await Run(dryRun: true)).IsFailure.ShouldBeFalse();

        File.Exists(Override).ShouldBeFalse();
    }
}

// Against the real compose: that it merges the override by itself when handed no file, and that
// the environment merges into a service that spells its own as a list, are what the step relies on.
