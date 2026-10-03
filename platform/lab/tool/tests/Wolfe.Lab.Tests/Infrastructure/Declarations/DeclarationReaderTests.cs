using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Tests.Infrastructure.Declarations;

// Against a real repository: what git tracks, and what a file's first line says, decide what is read.
public class DeclarationReaderTests : IDisposable
{
    private readonly DirectoryInfo _repository = Directory.CreateTempSubdirectory("lab-catalog-");

    public DeclarationReaderTests()
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

    private async Task<Result<Catalog>> Read() =>
        (await DeclarationReader.Read(new ProcessCommandRunner(), _repository.FullName, TestContext.Current.CancellationToken));

    private async Task<IReadOnlyList<string>> Errors() => [.. (await Read()).Errors.ShouldNotBeNull().Select(error => error.Message)];

    private const string Immich = "kind: service\nname: immich\ndescription: The photo library.\n";

    [Fact]
    public async Task Read_MakesTheCatalogOfWhatIsDeclared()
    {
        Write("personal/immich/RUNBOOK.md", "# Runbook");
        Declare("personal/immich/service.yaml", Immich + "links:\n  - { title: Runbook, type: runbook, path: RUNBOOK.md }\n");
        Declare("personal/immich/compose/component.yaml", "kind: workload\ntype: compose\n");

        var service = (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem();

        service.Declaration.Lifecycle.ShouldBe(Lifecycle.Production);
        service.Declaration.Links.ShouldHaveSingleItem().Target.ShouldBe(new Uri("RUNBOOK.md", UriKind.Relative));
        service.Components.ShouldHaveSingleItem().Declaration.Type.ShouldBe(ComponentType.Compose);
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
        Declare("personal/immich/vaults.yaml", "kind: backup\ntype: git\nname: main\n---\nkind: backup\ntype: git\nname: Main\n");

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

        var link = (await Read()).Value.ShouldNotBeNull().Services.ShouldHaveSingleItem().Declaration.Links.ShouldHaveSingleItem();

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

        (await DeclarationReader.Read(commands, _repository.FullName, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull()
            .Services.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Read_HoldsTheDocumentsTogetherToTheCatalogsRules()
    {
        Declare("personal/immich/compose/component.yaml", "kind: workload\ntype: compose\n");

        (await Errors()).ShouldHaveSingleItem().ShouldContain("personal/immich/ declares none");
    }
}
