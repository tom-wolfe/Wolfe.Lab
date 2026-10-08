using Ritten.Engine;
using Ritten.Engine.FileSystem;
using Ritten.Engine.Workflows;
using Ritten.Git;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.CaddyRoutes;
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
        await ResolveServiceCatalog.Check(RealClients.Git, new PhysicalDirectory(_repository.FullName), new PhysicalDirectory(Path.Combine(_repository.FullName, directory)), workflow,
            TestContext.Current.CancellationToken);

    private async Task<IReadOnlyList<string>> Errors(string directory, string workflow) =>
        [.. (await Check(directory, workflow)).Errors.ShouldNotBeNull().Select(error => error.Message)];

    [Theory]
    [InlineData("name: server\nkind: app\nworkflow: docker\nservice: server\n", "docker")]
    [InlineData("name: server\nkind: app\nworkflow: dotnet-service\nservice: server\n", "dotnet-service")]
    [InlineData("name: server\nkind: infrastructure\nworkflow: tofu\n", "tofu")]
    public async Task Check_PassesADeclarationThatSaysWhatItsWorkflowRuns(string declaration, string workflow)
    {
        Declare("personal/immich/compose/component.yaml", declaration);

        (await Check("personal/immich/compose", workflow)).Value.ShouldNotBeNull()
            .DeploymentUnitAt(RepositoryPath.From("personal/immich/compose")).ShouldNotBeNull().Value.ShouldNotBeNull().Components.ShouldHaveSingleItem().Name.Value.ShouldBe("server");
    }

    [Fact]
    public async Task Check_PassesADirectoryWithNoDeclarationYet() =>
        (await Check("personal/immich/compose", "docker")).Value.ShouldNotBeNull().DeploymentUnitAt(RepositoryPath.From("personal/immich/compose")).ShouldBeNull();

    [Fact]
    public async Task Check_RefusesADeclarationThatSaysOtherwise()
    {
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: infrastructure\nworkflow: tofu\n");

        (await Errors("personal/immich/compose", "docker")).ShouldHaveSingleItem()
            .ShouldBe("personal/immich/compose/component.yaml: declares the tofu workflow, but its directory's ritten.json runs docker.");
    }

    [Fact]
    public async Task Check_LeavesAPartToNameItsOwnWorkflow()
    {
        Declare("personal/immich/compose/component.yaml",
            "name: server\nkind: app\nworkflow: docker\nservice: server\n---\nname: snapshots\nkind: repository\nworkflow: tofu\npartOf: server\n");

        (await Check("personal/immich/compose", "docker")).IsSuccess.ShouldBeTrue();
    }

    // A catalog is valid or it is not: a problem anywhere fails every check, as it would every deploy.
    [Fact]
    public async Task Check_FailsOnAProblemAnywhereInTheCatalog()
    {
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\n");
        Declare("personal/immich/backup/component.yaml", "name: backup\nkind: storage\nworkflow: tofu\nvolumes: [/Volumes/Data2]\n");

        (await Errors("personal/immich/compose", "docker")).ShouldHaveSingleItem().ShouldContain("'volumes' is not something a tofu component declares");
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
        Declare("personal/immich/vaults.yaml", "kind: backup\nworkflow: heartbeat\nname: main\n");

        (await Check("personal/immich/compose", "docker")).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Check_JudgesAFileBesideTheEntryThatDoesNotReadAsAnything()
    {
        Declare("personal/immich/compose/component.yaml", "name: server\nkind: app\nworkflow: docker\nservice: server\n");
        Declare("personal/immich/notes.yaml", "kind: [unclosed\n");

        (await Errors("personal/immich/compose", "docker")).ShouldHaveSingleItem().ShouldContain("personal/immich/notes.yaml: line");
    }

    // A workflow whose label is not its name: caddy routes, as a run prints it, is caddy-routes.
    [Fact]
    public async Task Run_HoldsTheDeclarationToTheWorkflowsNameNotItsLabel()
    {
        Declare("personal/immich/routes/component.yaml", "name: routes\nkind: proxy\nworkflow: caddy-routes\n");
        var git = Substitute.For<IGit>();
        git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory(_repository.FullName));
        git.InRepository(Arg.Any<IDirectory>()).Returns(call => RealClients.Git.InRepository(call.Arg<IDirectory>()));
        var fileSystem = Substitute.For<IFileSystem>();
        var directory = Path.Combine(_repository.FullName, "personal/immich/routes");
        fileSystem.ProjectRoot.Returns(new PhysicalDirectory(directory));
        var selected = new SelectedWorkflow(new CaddyRoutesWorkflow(), RittenProject.Synthetic(directory, "ritten.json"));

        var result = await new ResolveServiceCatalog(git, fileSystem, selected, Substitute.For<IWorkflowLog>()).Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
    }
}
