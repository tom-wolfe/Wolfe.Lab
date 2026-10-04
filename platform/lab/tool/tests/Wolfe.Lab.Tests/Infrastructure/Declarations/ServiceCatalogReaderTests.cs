using Ritten.Engine.FileSystem;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Domain.Catalog.Facets.Telemetry;
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
        (await ServiceCatalogReader.Read(new ProcessCommandRunner(), new PhysicalDirectory(_repository.FullName), TestContext.Current.CancellationToken));

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
    public async Task Read_TakesGitsListingAsRittensRunnerCapturesIt()
    {
        Declare("personal/immich/service.yaml", Immich);
        var commands = Substitute.For<ICommandRunner>();
        commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "personal/immich/service.yaml\0\n", ""));

        (await ServiceCatalogReader.Read(commands, new PhysicalDirectory(_repository.FullName), TestContext.Current.CancellationToken)).Value.ShouldNotBeNull()
            .Services.ShouldHaveSingleItem();
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

        var server = components.Single(component => component.Name.Value == "server").ShouldBeOfType<ComposeComponent>();
        server.ComposeService.ShouldBe(ComposeServiceName.From("immich-server"));
        server.Logs.ShouldBe(LogTransport.Otlp);
        server.Metrics.ShouldBe(new MetricsEndpoint(Port.From(8081), HttpPath.Metrics));
        components.Single(component => component.Name.Value == "postgres").ShouldBeOfType<ComposeComponent>()
            .Metrics.ShouldBe(new MetricsEndpoint(Port.From(9187), HttpPath.From("/stats")));
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
}
