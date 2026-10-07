using Microsoft.Extensions.Options;
using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Docker.Models;
using Wolfe.Lab.Application.Workflows.Docker.Steps;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Facets.Telemetry;
using Wolfe.Lab.Domain.Network;
using Wolfe.Lab.Infrastructure.Compose;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;
using YamlDotNet.Serialization;

namespace Wolfe.Lab.Tests.Application.Workflows.Docker.Steps;

public class LabelServicesTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-label-");
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly DirectoryInfo _installed;

    public LabelServicesTests()
    {
        var checkout = _root.CreateSubdirectory("checkout");
        var component = checkout.CreateSubdirectory("personal").CreateSubdirectory("mail").CreateSubdirectory("watcher");
        _installed = _root.CreateSubdirectory("installed").CreateSubdirectory("mail-watcher");
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(component.FullName));
        _docker.ComposeConfig(Arg.Any<IDirectory>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(new ComposeProject([
            new ComposeService("bridge", new Dictionary<string, string>(), new Dictionary<string, string?>(), [new ComposePort(8080, 8080, null, "tcp")]),
            new ComposeService("watcher", new Dictionary<string, string>(), new Dictionary<string, string?>(), [])
        ]));
    }

    public void Dispose() => _root.Delete(recursive: true);

    // The stack's two services, each its own component; the bridge's named apart from its service.
    private static readonly Catalogs.Declaration[] Plain =
        [Catalogs.Compose("proton", "bridge", ComponentKind.Backend), Catalogs.Compose("watcher", "watcher", ComponentKind.Backend) with { DependsOn = ["proton"] }];

    private static readonly Catalogs.Declaration[] Declaring =
    [
        Catalogs.Compose("proton", "bridge", ComponentKind.Backend, metrics: new MetricsEndpoint(Port.From(9090), HttpPath.From("/stats"))),
        Catalogs.Compose("watcher", "watcher", ComponentKind.Backend, logs: LogTransport.Otlp) with { DependsOn = ["proton"] }
    ];

    private Task<StepResult<ComposeBindings>> Run(bool dryRun = false, Catalogs.Declaration[]? components = null) =>
        new LabelServices(_docker, _fileSystem, Options.Create(new LabDirectories { Root = new PhysicalDirectory(Path.Combine(_root.FullName, "installed")) }),
                new WorkflowJob("docker", "deploy", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(Catalogs.Unit("personal/mail/watcher", components ?? Plain),
                ComposeEnvironment.Empty, TestContext.Current.CancellationToken);

    private string Override => Path.Combine(_installed.FullName, LabelServices.OverrideFile);

    [Fact]
    public async Task Run_LabelsEachServiceWithWhereItsOwnComponentLives()
    {
        (await Run()).Outcome.IsFailure.ShouldBeFalse();

        var text = await File.ReadAllTextAsync(Override, TestContext.Current.CancellationToken);
        text.ShouldNotContain("&"); // written out per service, not as anchors and aliases
        var written = new DeserializerBuilder().Build()
            .Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>>(text);
        var services = written["services"];
        services.Keys.ShouldBe(["bridge", "watcher"], ignoreOrder: true);
        foreach (var (service, component) in new[] { ("watcher", "watcher"), ("bridge", "proton") })
        {
            services[service]["labels"].ShouldBe(new Dictionary<string, string>
            {
                ["lab.area"] = "personal",
                ["lab.service"] = "mail",
                ["lab.component"] = component
            });
            services[service]["environment"]["OTEL_RESOURCE_ATTRIBUTES"].ShouldBe($"lab.area=personal,lab.service=mail,lab.component={component}");
        }
    }

    [Fact]
    public async Task Run_AsksComposeForTheStackInTheCheckout_FoundAsComposeFindsIt()
    {
        await Run();

        // The checkout's directory, never the release's: compose finds its own files there.
        await _docker.Received().ComposeConfig(
            Arg.Is<IDirectory>(directory => directory.AbsolutePath.EndsWith("watcher", StringComparison.Ordinal)),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_WritesWhatEachComponentAsksOfItsService()
    {
        var bindings = (await Run(components: Declaring)).Value.ShouldNotBeNull();

        var written = new DeserializerBuilder().IgnoreUnmatchedProperties().Build()
            .Deserialize<Dictionary<string, Dictionary<string, Written>>>(await File.ReadAllTextAsync(Override, TestContext.Current.CancellationToken))["services"];
        written["watcher"].Labels["lab.logs"].ShouldBe("otlp");
        written["watcher"].Ports.ShouldBeNull();
        // Metrics are the collector's to find in the deploy's target file, not in labels.
        written["bridge"].Labels.Keys.ShouldNotContain("lab.metrics.port");
        written["bridge"].Labels["lab.area"].ShouldBe("personal");
        written["bridge"].Ports.ShouldBe(["127.0.0.1:9090:9090"]);
        bindings.Bindings.Single(binding => binding.Service.Name == "bridge").Metrics.ShouldBe([new MetricsTarget(Port.From(9090), HttpPath.From("/stats"))]);
    }

    [Fact]
    public async Task Run_RefusesAStackItsComponentsDoNotBind()
    {
        var result = await Run(components: [Catalogs.Compose("watcher", "watcher")]);

        result.Outcome.IsFailure.ShouldBeTrue();
        File.Exists(Override).ShouldBeFalse();
    }

    [Fact]
    public async Task Run_WritesNothingWhenRehearsing()
    {
        (await Run(dryRun: true)).Outcome.IsFailure.ShouldBeFalse();

        File.Exists(Override).ShouldBeFalse();
    }

    private sealed class Written
    {
        [YamlMember(Alias = "labels")]
        public Dictionary<string, string> Labels { get; set; } = [];

        [YamlMember(Alias = "ports")]
        public List<string>? Ports { get; set; }
    }
}

// Against the real compose: that it merges the override by itself when handed no file, and that
// the environment merges into a service that spells its own as a list, are what the step relies on.
