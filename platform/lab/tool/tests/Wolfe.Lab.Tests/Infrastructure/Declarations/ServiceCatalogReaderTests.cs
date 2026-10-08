using Ritten.Engine.FileSystem;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Components.Backups;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Domain.Catalog.Components.Models;
using Wolfe.Lab.Domain.Catalog.Facets.Telemetry;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Network;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Tests.Infrastructure.Declarations;

// Against a real repository: what git tracks, and what a file's first line says, decide what is read.
public class ServiceCatalogReaderTests : IDisposable
{
    private readonly DirectoryInfo _repository = Directory.CreateTempSubdirectory("lab-catalog-");

    public ServiceCatalogReaderTests()
    {
        Git("init", "-q", "-b", "main");
    }

    public void Dispose() => _repository.Delete(recursive: true);

    private void Git(params string[] arguments) =>
        new ProcessCommandRunner().Run(Command.Create("git").WithArguments(arguments).InDirectory(_repository.FullName).ThrowOnError()).GetAwaiter().GetResult();

    // A declaration as an editor would want it: its first line names the schema, relative to itself.
    // A problem's line is the file's, which is where an editor goes, whichever document it is in.
    private void Declare(string path, string yaml, bool track = true) =>
        Write(path, $"{DeclarationFiles.SchemaLineFor(RepositoryPath.From(path))}\n{yaml}", track);

    private void Write(string path, string text, bool track = true)
    {
        var file = Path.Combine(_repository.FullName, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file) ?? _repository.FullName);
        File.WriteAllText(file, text);
        if (track)
        {
            Git("add", path);
        }
    }

    private async Task<Result<ServiceCatalog>> Read() =>
        (await ServiceCatalogReader.Read(RealClients.Git, new PhysicalDirectory(_repository.FullName), TestContext.Current.CancellationToken));

    private async Task<IReadOnlyList<string>> Errors() => [.. (await Read()).Errors.ShouldNotBeNull().Select(error => error.Message)];

    private const string Immich = "kind: service\nname: immich\ndescription: The photo library.\n";

    [Fact]
    public async Task Read_MakesTheCatalogOfWhatIsDeclared()
    {
        Write("personal/immich/RUNBOOK.md", "# Runbook");
        Declare("personal/immich/service.yaml", Immich + "links:\n  - { title: Runbook, type: runbook, path: RUNBOOK.md }\n");
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\n");

        var service = (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem();

        service.Lifecycle.ShouldBe(Lifecycle.Production);
        service.Links.ShouldHaveSingleItem().Target.ShouldBe(new Uri("RUNBOOK.md", UriKind.Relative));
        service.Components.ShouldHaveSingleItem().Workflow.ShouldBe(WorkflowName.Docker);
    }

    [Fact]
    public async Task Read_LeavesAloneWhatIsNotTheLabsOrNotTracked()
    {
        Write("personal/immich/compose/compose.yaml", "services: {}\n");
        Write("personal/immich/notes.yaml", "kind: service\n");
        Declare("personal/photos/service.yaml", "kind: nonsense\n", track: false);

        (await Read()).Value.ShouldNotBeNull().Services.ShouldBeEmpty();
    }

    [Fact]
    public async Task Read_RefusesASchemaLineThatDoesNotLeadToTheSchema()
    {
        Write("personal/immich/service.yaml", $"# yaml-language-server: $schema=../{LabSchema.Location}\n{Immich}");

        (await Errors()).ShouldHaveSingleItem().ShouldContain($"from here that is ../../{LabSchema.Location}");
    }

    [Fact]
    public async Task Read_PointsEachProblemAtItsFileDocumentAndLine()
    {
        Declare("personal/immich/service.yaml", Immich);
        Declare("personal/immich/vaults.yaml", "kind: backup\nworkflow: obsidian\nname: main\n---\nkind: backup\nworkflow: obsidian\nname: Main\n");

        (await Errors()).ShouldHaveSingleItem().ShouldStartWith("personal/immich/vaults.yaml#2:8: name: 'Main' is not a name");
    }

    [Fact]
    public async Task Read_RefusesALinkToAPathThatIsNotThere()
    {
        Declare("personal/immich/service.yaml", Immich + "links:\n  - { title: Runbook, type: runbook, path: RUNBOOK.md }\n");

        (await Errors()).ShouldHaveSingleItem().ShouldContain("the path 'RUNBOOK.md', which is not in the checkout");
    }

    [Fact]
    public async Task Read_HoldsAUrlLinkAsTheAbsoluteUriItIs()
    {
        Declare("personal/immich/service.yaml", Immich + "links:\n  - { title: Immich, type: app, url: https://immich.twolfe.dev }\n");

        var link = (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem().Links.ShouldHaveSingleItem();

        link.Target.IsAbsoluteUri.ShouldBeTrue();
        link.Target.Host.ShouldBe("immich.twolfe.dev");
    }

    [Theory]
    [InlineData("{ title: Home, type: app }", "needs a url or a path")]
    [InlineData("{ title: Home, type: app, path: /Users/tomwolfe }", "not a relative path")]
    [InlineData("{ title: Home, type: app, path: ../../../.. }", "leads out of the checkout")]
    [InlineData("{ title: Home, type: app, url: immich.twolfe.dev }", "not an absolute http or https URL")]
    public async Task Read_HoldsALinkToAUrlOrAPath(string link, string expected)
    {
        Declare("personal/immich/service.yaml", Immich + $"links:\n  - {link}\n");

        (await Errors()).ShouldHaveSingleItem().ShouldContain(expected);
    }

    [Fact]
    public async Task Read_PassesOverAFileGitTracksButTheWorkingDirectoryHasLost()
    {
        Declare("personal/immich/service.yaml", Immich);
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\n");
        File.Delete(Path.Combine(_repository.FullName, "personal/immich/compose/component.yaml"));

        (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem().Components.ShouldBeEmpty();
    }

    [Fact]
    public async Task Read_TakesEachDocumentAsItsWorkflowsShape()
    {
        Declare("personal/immich/service.yaml", Immich);
        Declare("personal/immich/compose/component.yaml", """
            name: server
            kind: app
            workflow: docker
            service: immich-server
            dependsOn: [postgres]
            logs: otlp
            metrics: { port: 8081 }
            ---
            name: postgres
            kind: database
            workflow: docker
            service: immich-database
            metrics: { port: 9187, path: /stats }
            ---
            name: database
            kind: database
            workflow: restic
            partOf: postgres
            """);

        var components = (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem().Components;

        var server = components.Single(component => component.Name.Value == "server").ShouldBeOfType<DockerComponent>();
        server.ComposeService.ShouldBe(ComposeServiceName.From("immich-server"));
        server.Logs.ShouldBe(LogTransport.Otlp);
        server.Metrics.ShouldBe([new MetricsEndpoint(Port.From(8081), HttpPath.Metrics)]);
        components.Single(component => component.Name.Value == "postgres").ShouldBeOfType<DockerComponent>()
            .Metrics.ShouldBe([new MetricsEndpoint(Port.From(9187), HttpPath.From("/stats"))]);
        var database = components.Single(component => component.Name.Value == "database");
        database.GetType().ShouldBe(typeof(Component));
        database.Kind.ShouldBe(ComponentKind.Database);
        database.Host.ShouldNotBeNull().Name.Value.ShouldBe("postgres");
    }

    [Fact]
    public async Task Read_RefusesWhatAWorkflowsShapeDoesNotHold()
    {
        Declare("personal/immich/service.yaml", Immich);
        Declare("personal/immich/compose/component.yaml", """
            name: server
            kind: app
            workflow: docker
            service: server
            logs: stdout
            metrics: { port: 70000 }
            ---
            name: database
            kind: database
            workflow: restic
            metrics: { port: 8081 }
            """);

        // The shape first: a document the schema refuses is read no further.
        (await Errors()).ShouldBe([
            "personal/immich/compose/component.yaml#1:6: logs: 'stdout' is not a way logs are delivered (otlp).",
            "personal/immich/compose/component.yaml#1:7: metrics.port: 70000 should be at most 65535",
            "personal/immich/compose/component.yaml#2:12: metrics: 'metrics' is not something a restic component declares."
        ]);

        Declare("personal/immich/compose/component.yaml", """
            name: server
            kind: app
            workflow: docker
            service: _server
            metrics: { port: 8081, path: metrics }
            """);

        (await Errors()).ShouldBe([
            "personal/immich/compose/component.yaml: service: '_server' is not a compose service's name: a letter or digit first, then letters, digits, '_', '.' and '-'.",
            "personal/immich/compose/component.yaml: metrics.path: 'metrics' is not a path: it starts with '/', and has no spaces, query or fragment."
        ], ignoreOrder: true);
    }

    // Read in whatever order git lists them, added in the order a valid catalog needs.
    [Fact]
    public async Task Read_AddsWhatADeclarationDependsOnBeforeIt_WhereverItIsRead()
    {
        Declare("personal/immich/service.yaml", Immich + "dependsOn: [garage]\n");
        Declare("platform/garage/service.yaml", "kind: service\nname: garage\ndescription: Object storage.\n");
        Declare("personal/immich/compose/component.yaml", """
            name: database
            kind: database
            workflow: restic
            partOf: postgres
            ---
            name: postgres
            kind: database
            workflow: docker
            service: immich-database
            """);

        var services = (await Read()).Value.ShouldNotBeNull().Services;

        services.Single(service => service.Name.Value == "immich").DependsOn.ShouldBe([ServiceName.From("garage")]);
        services.Single(service => service.Name.Value == "immich").Components.Single(component => component.Name.Value == "database")
            .PartOf.ShouldBe(ComponentName.From("postgres"));
    }

    [Fact]
    public async Task Read_RefusesServicesThatDependOnEachOther()
    {
        Declare("personal/immich/service.yaml", Immich + "dependsOn: [garage]\n");
        Declare("platform/garage/service.yaml", "kind: service\nname: garage\ndescription: Object storage.\ndependsOn: [immich]\n");

        (await Errors()).Count.ShouldBe(2);
        (await Errors()).ShouldAllBe(error => error.Contains("which depends on this one in turn"));
    }

    [Fact]
    public async Task Read_HoldsTheDocumentsTogetherToTheCatalogsRules()
    {
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\n");

        (await Errors()).ShouldHaveSingleItem().ShouldContain("personal/immich/ declares none");
    }

    private const string Nodes = """
        kind: node
        name: mini
        role: server
        platform: darwin-arm64
        address: macmini.tailnet.ts.net
        root: /Users/me/.local/share/Wolfe.Lab
        data: /Users/me/Docker
        docker: unix:///Users/me/.docker/run/docker.sock
        drives: [/Volumes/Data1, /Volumes/Data2]
        ---
        kind: node
        name: pi
        role: server
        platform: linux-arm64
        address: wolfe-pi5.tailnet.ts.net
        root: /home/me/.local/share/Wolfe.Lab
        data: /home/me/Docker
        docker: unix:///var/run/docker.sock

        """;

    private const string Alloy = "kind: service\nname: alloy\ndescription: The collector.\n";

    [Fact]
    public async Task Read_TakesTheNodesFromPlatform()
    {
        Declare("platform/nodes.yaml", Nodes);

        var nodes = (await Read()).Value.ShouldNotBeNull().Nodes;

        nodes.Select(node => node.Name.Value).ShouldBe(["mini", "pi"]);
        nodes[0].Drives.Select(drive => drive.Value).ShouldBe(["/Volumes/Data1", "/Volumes/Data2"]);
        nodes[1].Platform.ShouldBe(NodePlatform.LinuxArm64);
        nodes[1].Directories.Data.ShouldBe(AbsolutePath.From("/home/me/Docker"));
    }

    [Fact]
    public async Task Read_PlacesAnAgentComponentOnTheNodes()
    {
        Declare("platform/nodes.yaml", Nodes);
        Declare("monitoring/alloy/service.yaml", Alloy);
        Declare("monitoring/alloy/forwarder/component.yaml", """
            name: forwarder
            kind: collector
            workflow: agent
            runsOn: all
            agent: alloy
            package: { github: grafana/alloy, version: 1.20.1, asset: "alloy-{platform}.zip", checksums: SHA256SUMS }
            program: "{package}/alloy-{platform}"
            arguments: [run, "{lab.root}/alloy/forwarder.alloy"]
            environment:
              LAB_GATEWAY: "{node.mini.address}"

            """);

        var component = (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem().Components.ShouldHaveSingleItem().ShouldBeOfType<AgentComponent>();

        component.RunsOn.ToString().ShouldBe("all");
        component.Agent.Name.Value.ShouldBe("alloy");
        component.Agent.Package.ShouldNotBeNull().Checksums.ShouldBe(Template.From("SHA256SUMS"));
        component.Agent.Environment["LAB_GATEWAY"].ShouldBe(Template.From("{node.mini.address}"));
    }

    [Fact]
    public async Task Read_PlacesAnAgentComponentOnTheNodesItNames()
    {
        Declare("platform/nodes.yaml", Nodes);
        Declare("monitoring/alloy/service.yaml", Alloy);
        Declare("monitoring/alloy/forwarder/component.yaml", "name: forwarder\nkind: collector\nworkflow: agent\nrunsOn: [pi]\nagent: alloy\nprogram: /usr/bin/alloy\n");

        var component = (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem().Components.ShouldHaveSingleItem().ShouldBeOfType<AgentComponent>();

        component.RunsOn.Named.ShouldBe([NodeName.From("pi")]);
    }

    [Fact]
    public async Task Read_RefusesAnAgentComponentThatDeclaresNoAgent()
    {
        Declare("monitoring/alloy/service.yaml", Alloy);
        Declare("monitoring/alloy/forwarder/component.yaml", "name: forwarder\nkind: collector\nworkflow: agent\nrunsOn: all\n");

        (await Errors()).ShouldBe(["monitoring/alloy/forwarder/component.yaml:2: Required properties [\"agent\",\"program\"] are not present"]);
    }

    [Fact]
    public async Task Read_RefusesAPlacementOnANodeTheLabDoesNotDeclare()
    {
        Declare("platform/nodes.yaml", Nodes);
        Declare("monitoring/alloy/service.yaml", Alloy);
        Declare("monitoring/alloy/forwarder/component.yaml", "name: forwarder\nkind: collector\nworkflow: agent\nrunsOn: [studio]\nagent: alloy\nprogram: /usr/bin/alloy\n");

        (await Errors()).ShouldHaveSingleItem().ShouldBe("monitoring/alloy/forwarder/component.yaml: runsOn: names the node 'studio', which the lab does not declare.");
    }

    [Fact]
    public async Task Read_RefusesANodeOutsidePlatform()
    {
        Declare("monitoring/nodes.yaml", Nodes);

        (await Errors()).ShouldAllBe(error => error.Contains("a node is declared in platform/"));
    }

    [Fact]
    public async Task Read_RefusesANodesAddressDockerAndDirectoriesThatAreNotThem()
    {
        Declare("platform/nodes.yaml", "kind: node\nname: mini\nrole: server\nplatform: darwin-arm64\naddress: http://macmini\nroot: ~/.local/share/Wolfe.Lab\ndata: /Users/me/Docker\ndocker: /var/run/docker.sock\n");

        (await Errors()).Select(error => error.Split(": ")[1]).ShouldBe(["address", "docker", "root"]);
    }

    [Fact]
    public async Task Read_RefusesAPackageVersionThatIsNotOne()
    {
        Declare("platform/nodes.yaml", Nodes);
        Declare("monitoring/alloy/service.yaml", Alloy);
        Declare("monitoring/alloy/forwarder/component.yaml",
            "name: forwarder\nkind: collector\nworkflow: agent\nrunsOn: all\nagent: alloy\nprogram: \"{package}/alloy\"\n"
            + "package: { github: grafana/alloy, version: ../1.20.1, asset: alloy.zip }\n");

        (await Errors()).ShouldHaveSingleItem().ShouldStartWith("monitoring/alloy/forwarder/component.yaml: package.version: '../1.20.1' is not a version");
    }

    private const string Ollama = "kind: service\nname: ollama\ndescription: The model endpoint.\n";

    [Fact]
    public async Task Read_RefusesAServerDeclaredAsOllama_ForWhichItIsAnAgent()
    {
        Declare("platform/nodes.yaml", Nodes);
        Declare("ai/ollama/service.yaml", Ollama);
        Declare("ai/ollama/mini/component.yaml", "name: mini\nkind: model\nworkflow: ollama\nrunsOn: [mini]\nagent: ollama\nprogram: \"{package}/ollama\"\n");

        (await Errors()).ShouldNotBeEmpty();
    }

    // The service's two servers, each an agent placed on its node.
    private void Servers()
    {
        Declare("platform/nodes.yaml", Nodes);
        Declare("ai/ollama/service.yaml", Ollama);
        Declare("ai/ollama/mini/component.yaml", "name: mini\nkind: model\nworkflow: agent\nrunsOn: [mini]\nagent: ollama\nprogram: \"{package}/ollama\"\n");
        Declare("ai/ollama/pi/component.yaml", "name: pi\nkind: model\nworkflow: agent\nrunsOn: [pi]\nagent: ollama\nprogram: /usr/bin/ollama\n");
    }

    [Fact]
    public async Task Read_TakesAModelByItsUse_EachServerItsDefaultsOrItsOwn()
    {
        Servers();
        Declare("ai/ollama/interactive/component.yaml", """
            name: interactive
            kind: model
            workflow: ollama
            model: "qwen3.6:35b-a3b"
            context: 16384
            servedBy:
              mini: { model: "qwen3.5:9b", context: 8192 }
              pi: {}
            """);

        var model = (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem().Components.OfType<ModelComponent>().ShouldHaveSingleItem();

        model.Alias.ShouldBe("lab/interactive");
        (model.ModelOn(ComponentName.From("mini")), model.ContextOn(ComponentName.From("mini"))).ShouldBe((ModelTag.From("qwen3.5:9b"), ContextLength.From(8192)));
        (model.ModelOn(ComponentName.From("pi")), model.ContextOn(ComponentName.From("pi"))).ShouldBe((ModelTag.From("qwen3.6:35b-a3b"), ContextLength.From(16384)));
    }

    [Fact]
    public async Task Read_RefusesAServerWithNoModelForAUse()
    {
        Servers();
        Declare("ai/ollama/interactive/component.yaml", "name: interactive\nkind: model\nworkflow: ollama\nservedBy: { mini: { model: \"qwen3.5:9b\" }, pi: {} }\n");

        (await Errors()).ShouldHaveSingleItem().ShouldContain("servedBy.pi: 'pi' has no model for it");
    }

    [Fact]
    public async Task Read_RefusesAModelSomeServersDoNotServe()
    {
        Servers();
        Declare("ai/ollama/interactive/component.yaml", "name: interactive\nkind: model\nworkflow: ollama\nmodel: \"qwen3.5:9b\"\nservedBy: { mini: {}, pi: {} }\n");
        Declare("ai/ollama/embedding/component.yaml", "name: embedding\nkind: model\nworkflow: ollama\nmodel: \"embeddinggemma:300m\"\nservedBy: { mini: {} }\n");

        (await Errors()).ShouldHaveSingleItem().ShouldContain("is not served by pi");
    }

    [Fact]
    public async Task Read_RefusesAModelServedByWhatIsNoServer()
    {
        Servers();
        Declare("ai/ollama/interactive/component.yaml", "name: interactive\nkind: model\nworkflow: ollama\nmodel: \"qwen3.5:9b\"\nservedBy: { mini: {}, pi: {}, studio: {} }\n");

        (await Errors()).ShouldContain(error => error.Contains("'studio' is not one of its service's servers"));
    }

    [Fact]
    public async Task Read_TakesSeveralMetricsEndpoints_AndThePortsTheyArePublishedOn()
    {
        Declare("personal/immich/service.yaml", Immich);
        Declare("personal/immich/compose/component.yaml", """
            name: server
            kind: app
            workflow: docker
            service: immich-server
            metrics:
              - { port: 8081 }
              - { port: 8082, path: /stats, published: 18082 }

            """);

        var server = (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem().Components.ShouldHaveSingleItem().ShouldBeOfType<DockerComponent>();

        server.Metrics.ShouldBe([
            new MetricsEndpoint(Port.From(8081), HttpPath.Metrics),
            new MetricsEndpoint(Port.From(8082), HttpPath.From("/stats")) { Published = Port.From(18082) }
        ]);
    }

    [Fact]
    public async Task Read_TakesABackupAsPartOfWhatItStops_WithTheVolumesItRequires()
    {
        Declare("media/sonarr/service.yaml", "kind: service\nname: sonarr\ndescription: TV.\n");
        Declare("media/sonarr/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: sonarr\nrequiresVolumes: [/Volumes/Data1]\n");
        Declare("media/sonarr/backup/component.yaml", """
            name: config
            kind: storage
            workflow: backup
            partOf: server
            paths: [/Users/lab/Docker/sonarr/config]
            excludes: [/Users/lab/Docker/sonarr/config/logs]
            verify: [/Users/lab/Docker/sonarr/config/sonarr.db]
            """);

        var service = (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem();

        var backup = service.FindComponent(ComponentName.From("config")).ShouldBeOfType<BackupComponent>();
        backup.Paths.ShouldBe([HostPath.From("/Users/lab/Docker/sonarr/config")]);
        backup.Excludes.ShouldBe([HostPath.From("/Users/lab/Docker/sonarr/config/logs")]);
        backup.Verify.ShouldBe([HostPath.From("/Users/lab/Docker/sonarr/config/sonarr.db")]);
        backup.Warm.ShouldBeFalse();
        backup.Stops.ShouldBeSameAs(service.FindComponent(ComponentName.From("server")));
        service.FindComponent(ComponentName.From("server")).ShouldNotBeNull().RequiresVolumes.ShouldBe([HostPath.From("/Volumes/Data1")]);
    }

    [Fact]
    public async Task Read_RefusesABackupThatSnapshotsNothing()
    {
        Declare("personal/files/service.yaml", "kind: service\nname: files\ndescription: Files.\n");
        Declare("personal/files/backup/component.yaml", "name: files\nkind: backup\nworkflow: backup\npaths: []\n");

        (await Errors()).ShouldHaveSingleItem().ShouldStartWith("personal/files/backup/component.yaml:");
    }

    [Fact]
    public async Task Read_RefusesAVolumeThatIsNoPath()
    {
        Declare("personal/immich/service.yaml", Immich);
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\nrequiresVolumes: [Data2]\n");

        (await Errors()).ShouldHaveSingleItem().ShouldContain("requiresVolumes.0: ");
    }

    [Fact]
    public async Task Read_RefusesAStackWithSeveralAtItsHead()
    {
        Declare("personal/immich/service.yaml", Immich);
        Declare("personal/immich/compose/component.yaml", """
            name: server
            kind: app
            workflow: docker
            service: immich-server
            ---
            name: machine-learning
            kind: model
            workflow: docker
            service: immich-machine-learning
            """);

        (await Errors()).ShouldBe([
            "personal/immich/compose/component.yaml: personal/immich/compose has 2 components at its head (machine-learning, server): exactly one is, every other its dependency or its part, and it names the deployment."
        ]);
    }

    [Fact]
    public async Task Read_TakesADotNetServiceAsOneBuiltFromSource()
    {
        Declare("personal/mail/service.yaml", "kind: service\nname: mail\ndescription: Mail.\n");
        Declare("personal/mail/watcher/component.yaml", "name: watcher\nkind: backend\nworkflow: dotnet-service\nservice: watcher\nlogs: otlp\n");

        var watcher = (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem().Components.ShouldHaveSingleItem().ShouldBeOfType<DotNetServiceComponent>();

        watcher.Image.ShouldBe("lab/mail-watcher");
        watcher.Logs.ShouldBe(LogTransport.Otlp);
    }
}
