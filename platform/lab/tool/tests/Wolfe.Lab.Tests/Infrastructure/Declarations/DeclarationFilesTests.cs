using Ritten.Engine.FileSystem;
using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Tests.Infrastructure.Declarations;

public sealed class DeclarationFilesTests : IDisposable
{
    private const string Schema = "# yaml-language-server: $schema=../../platform/lab/schema/lab.schema.json\n";

    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("declarations");

    [Fact]
    public async Task WorkflowOf_IsTheWorkflowTheDirectorysComponentsDeclare()
    {
        Write("component.yaml", Schema + "name: server\nkind: app\nworkflow: docker\nservice: server\n");

        (await WorkflowOf()).ShouldBe("docker");
    }

    [Fact]
    public async Task WorkflowOf_LeavesOutAPartOfAnother()
    {
        Write("component.yaml", Schema + "name: server\nkind: app\nworkflow: docker\nservice: server\n---\nname: backup\nkind: backup\nworkflow: backup\npartOf: server\npaths: [/data]\n");

        (await WorkflowOf()).ShouldBe("docker");
    }

    [Fact]
    public async Task WorkflowOf_TakesAPartOfAComponentElsewhereAsTheDirectorys()
    {
        Write("component.yaml", Schema + "name: backup\nkind: backup\nworkflow: backup\npartOf: server\npaths: [/data]\n");

        (await WorkflowOf()).ShouldBe("backup");
    }

    [Fact]
    public async Task WorkflowOf_IsNothingWhenComponentsDeclareSeveral()
    {
        Write("component.yaml", Schema + "name: server\nkind: app\nworkflow: docker\nservice: server\n---\nname: infrastructure\nkind: infrastructure\nworkflow: tofu\n");

        (await WorkflowOf()).ShouldBeNull();
    }

    [Fact]
    public async Task WorkflowOf_ReadsOnlyTheLabsDeclarations()
    {
        Write("compose.yaml", "name: server\nworkflow: docker\n");

        (await WorkflowOf()).ShouldBeNull();
    }

    [Fact]
    public async Task WorkflowOf_IsNothingWithNoDeclarations() =>
        (await WorkflowOf()).ShouldBeNull();

    public void Dispose() => _directory.Delete(recursive: true);

    private void Write(string name, string text) => File.WriteAllText(Path.Combine(_directory.FullName, name), text);

    private Task<string?> WorkflowOf() => DeclarationFiles.WorkflowOf(new PhysicalDirectory(_directory.FullName), TestContext.Current.CancellationToken);
}
