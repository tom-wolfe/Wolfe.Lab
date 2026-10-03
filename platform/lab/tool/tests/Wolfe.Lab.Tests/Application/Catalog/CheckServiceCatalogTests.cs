using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Tests.Application.Catalog;

// Against a real repository: the component's check reads every declaration, and judges its own.
public class CheckServiceCatalogTests : IDisposable
{
    private readonly DirectoryInfo _repository = Directory.CreateTempSubdirectory("lab-check-declaration-");

    public CheckServiceCatalogTests()
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

    private async Task<Result<CheckServiceCatalog.Declared>> Check(string directory, string workflow) =>
        await CheckServiceCatalog.Check(new ProcessCommandRunner(), _repository.FullName, Path.Combine(_repository.FullName, directory), workflow,
            TestContext.Current.CancellationToken);

    private async Task<IReadOnlyList<string>> Errors(string directory, string workflow) =>
        [.. (await Check(directory, workflow)).Errors.ShouldNotBeNull().Select(error => error.Message)];

    [Theory]
    [InlineData("kind: workload\ntype: compose\n", "docker")]
    [InlineData("kind: workload\ntype: compose\n", "dotnet-service")]
    [InlineData("kind: workload\ntype: agent\n", "agents")]
    public async Task Check_PassesADeclarationThatSaysWhatItsWorkflowRuns(string declaration, string workflow)
    {
        Declare("personal/immich/compose/component.yaml", declaration);

        (await Check("personal/immich/compose", workflow)).Value.ShouldNotBeNull().Component.ShouldNotBeNull().Name.Value.ShouldBe("compose");
    }

    [Fact]
    public async Task Check_PassesAComponentWithNoDeclarationYet() =>
        (await Check("personal/immich/compose", "docker")).Value.ShouldNotBeNull().Component.ShouldBeNull();

    [Fact]
    public async Task Check_RefusesADeclarationThatSaysOtherwise()
    {
        Declare("personal/immich/compose/component.yaml", "kind: workload\ntype: agent\n");

        (await Errors("personal/immich/compose", "docker")).ShouldHaveSingleItem()
            .ShouldBe("personal/immich/compose/component.yaml: declares workload/agent, but its ritten.json runs the docker workflow, which is workload/compose.");
    }

    [Fact]
    public async Task Check_RefusesADeclarationBesideAWorkflowThatBecomesAFacet()
    {
        Declare("personal/immich/health/component.yaml", "kind: workload\ntype: compose\n");

        (await Errors("personal/immich/health", "gatus-health")).ShouldHaveSingleItem().ShouldContain("runs gatus-health, but the gatus-health workflow becomes a facet");
    }

    [Fact]
    public async Task Check_JudgesOnlyItsOwnDeclaration()
    {
        Declare("personal/immich/compose/component.yaml", "kind: workload\ntype: compose\n");
        Declare("personal/immich/backup/component.yaml", "kind: backup\ntype: snapshot\nvolumes: [/Volumes/Data2]\n");

        (await Check("personal/immich/compose", "docker")).IsSuccess.ShouldBeTrue();
        (await Errors("personal/immich/backup", "backup")).ShouldHaveSingleItem().ShouldContain("'volumes' is not something a backup declares");
    }

    [Fact]
    public async Task Check_ReadsEveryDeclarationToResolveWhatItsOwnNames()
    {
        Declare("personal/immich/compose/component.yaml", "kind: workload\ntype: compose\ndependsOn: [database]\n");
        Declare("personal/immich/database/component.yaml", "kind: workload\ntype: compose\n");

        (await Check("personal/immich/compose", "docker")).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Check_JudgesItsServicesEntryAndSaysWhatThatMeansForIt()
    {
        Declare("personal/immich/service.yaml", "kind: service\nname: immich\n");
        Declare("personal/immich/compose/component.yaml", "kind: workload\ntype: compose\n");

        (await Errors("personal/immich/compose", "docker")).ShouldBe([
            "personal/immich/service.yaml:2: the document: Required properties [\"description\"] are not present",
            "personal/immich/compose/component.yaml: a component belongs to a service, and personal/immich/ declares none that holds."
        ]);
    }

    [Fact]
    public async Task Check_LeavesAComponentDeclaredBesideTheEntryToItsOwnCheck()
    {
        Declare("personal/immich/compose/component.yaml", "kind: workload\ntype: compose\n");
        Declare("personal/immich/vaults.yaml", "kind: backup\ntype: git\nname: Main\n");

        (await Check("personal/immich/compose", "docker")).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Check_JudgesAFileBesideTheEntryThatDoesNotReadAsAnything()
    {
        Declare("personal/immich/compose/component.yaml", "kind: workload\ntype: compose\n");
        Declare("personal/immich/notes.yaml", "kind: [unclosed\n");

        (await Errors("personal/immich/compose", "docker")).ShouldHaveSingleItem().ShouldContain("personal/immich/notes.yaml: line");
    }
}
