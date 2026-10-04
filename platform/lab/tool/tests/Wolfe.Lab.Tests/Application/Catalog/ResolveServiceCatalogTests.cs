using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Tests.Application.Catalog;

// Against a real repository: the component's check reads every declaration, and judges its own.
public class ResolveServiceCatalogTests : IDisposable
{
    private readonly DirectoryInfo _repository = Directory.CreateTempSubdirectory("lab-check-declaration-");

    public ResolveServiceCatalogTests()
    {
        Git("init", "-q", "-b", "main");
        Declare("personal/immich/service.yaml", "kind: service\nname: immich\ndescription: Photos.\n");
    }

    public void Dispose() => _repository.Delete(recursive: true);

    private void Git(params string[] arguments) =>
        new ProcessCommandRunner().Run(Command.Create("git").WithArguments(arguments).InDirectory(_repository.FullName).ThrowOnError()).GetAwaiter().GetResult();

    private void Declare(string path, string yaml)
    {
        var file = Path.Combine(_repository.FullName, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file) ?? _repository.FullName);
        File.WriteAllText(file, $"{DeclarationFiles.SchemaLineFor(RepositoryPath.From(path))}\n{yaml}");
        Git("add", path);
    }

    private async Task<Result<ServiceCatalog>> Check(string directory, string workflow) =>
        await ResolveServiceCatalog.Check(new ProcessCommandRunner(), new PhysicalDirectory(_repository.FullName), new PhysicalDirectory(Path.Combine(_repository.FullName, directory)), workflow,
            TestContext.Current.CancellationToken);

    private async Task<IReadOnlyList<string>> Errors(string directory, string workflow) =>
        [.. (await Check(directory, workflow)).Errors.ShouldNotBeNull().Select(error => error.Message)];

    [Theory]
    [InlineData("name: server\nkind: app\nworkflow: docker\nservice: server\n", "docker")]
    [InlineData("name: server\nkind: app\nworkflow: dotnet-service\nservice: server\n", "dotnet-service")]
    [InlineData("name: server\nkind: model\nworkflow: agents\n", "agents")]
    public async Task Check_PassesADeclarationThatSaysWhatItsWorkflowRuns(string declaration, string workflow)
    {
        Declare("personal/immich/compose/component.yaml", declaration);

        (await Check("personal/immich/compose", workflow)).Value.ShouldNotBeNull()
            .DeploymentUnitAt(RepositoryPath.From("personal/immich/compose")).ShouldNotBeNull().Components.ShouldHaveSingleItem().Name.Value.ShouldBe("server");
    }

    [Fact]
    public async Task Check_PassesADirectoryWithNoDeclarationYet() =>
        (await Check("personal/immich/compose", "docker")).Value.ShouldNotBeNull().DeploymentUnitAt(RepositoryPath.From("personal/immich/compose")).ShouldBeNull();

    [Fact]
    public async Task Check_RefusesADeclarationThatSaysOtherwise()
    {
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: model\nworkflow: agents\n");

        (await Errors("personal/immich/compose", "docker")).ShouldHaveSingleItem()
            .ShouldBe("personal/immich/compose/component.yaml: declares the agents workflow, but its directory's ritten.json runs docker.");
    }

    [Fact]
    public async Task Check_RefusesAComponentInADirectoryWhoseWorkflowOperatesNone()
    {
        Declare("personal/immich/health/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\n");

        (await Errors("personal/immich/health", "gatus-health")).ShouldHaveSingleItem().ShouldContain("declares the docker workflow, but its directory's ritten.json runs gatus-health.");
    }

    [Fact]
    public async Task Check_LeavesAPartToNameItsOwnWorkflow()
    {
        Declare("personal/immich/compose/component.yaml",
            "name: server\nkind: app\nworkflow: docker\nservice: server\n---\nname: snapshots\nkind: repository\nworkflow: restic\npartOf: server\n");

        (await Check("personal/immich/compose", "docker")).IsSuccess.ShouldBeTrue();
    }

    // A catalog is valid or it is not: a problem anywhere fails every check, as it would every deploy.
    [Fact]
    public async Task Check_FailsOnAProblemAnywhereInTheCatalog()
    {
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\n");
        Declare("personal/immich/backup/component.yaml", "name: backup\nkind: storage\nworkflow: restic\nvolumes: [/Volumes/Data2]\n");

        (await Errors("personal/immich/compose", "docker")).ShouldHaveSingleItem().ShouldContain("'volumes' is not something a restic component declares");
    }

    [Fact]
    public async Task Check_ReadsEveryDeclarationToResolveWhatItsOwnNames()
    {
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\ndependsOn: [database]\n");
        Declare("personal/immich/database/component.yaml", "name: database\nkind: database\nworkflow: docker\nservice: database\n");

        (await Check("personal/immich/compose", "docker")).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Check_JudgesItsServicesEntryAndSaysWhatThatMeansForIt()
    {
        Declare("personal/immich/service.yaml", "kind: service\nname: immich\n");
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\n");

        (await Errors("personal/immich/compose", "docker")).ShouldBe([
            "personal/immich/service.yaml:2: Required properties [\"description\"] are not present",
            "personal/immich/compose/component.yaml: a component belongs to a service, and personal/immich/ declares none that holds."
        ]);
    }

    [Fact]
    public async Task Check_PassesAComponentBesideItsServicesEntry()
    {
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\n");
        Declare("personal/immich/vaults.yaml", "kind: backup\nworkflow: obsidian\nname: main\n");

        (await Check("personal/immich/compose", "docker")).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Check_JudgesAFileBesideTheEntryThatDoesNotReadAsAnything()
    {
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\n");
        Declare("personal/immich/notes.yaml", "kind: [unclosed\n");

        (await Errors("personal/immich/compose", "docker")).ShouldHaveSingleItem().ShouldContain("personal/immich/notes.yaml: line");
    }
}
