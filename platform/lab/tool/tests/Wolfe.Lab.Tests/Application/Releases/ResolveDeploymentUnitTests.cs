using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Releases;

public class ResolveDeploymentUnitTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-placement-");
    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ServiceCatalog _catalog = Catalogs.Of("ai/ollama/server", Catalogs.Definition("server", ComponentKind.Model, WorkflowName.Agent));

    public ResolveDeploymentUnitTests() =>
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory(_checkout.FullName));

    public void Dispose() => _checkout.Delete(recursive: true);

    private Task<StepResult<DeploymentUnit>> Resolve(params string[] path)
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(Directory.CreateDirectory(Path.Combine([_checkout.FullName, .. path])).FullName));
        return new ResolveDeploymentUnit(_git, _fileSystem, Substitute.For<IWorkflowLog>()).Run(_catalog, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_FindsWhatTheDirectoryDeclares()
    {
        var unit = (await Resolve("ai", "ollama", "server")).Value.ShouldNotBeNull();

        unit.Components.ShouldBe(_catalog.Services.ShouldHaveSingleItem().Components);
        unit.Components.ShouldHaveSingleItem().QualifiedName.ShouldBe("ai-ollama-server");
    }

    [Theory]
    [InlineData("ai", "ollama", "studio")]
    [InlineData("loose", "compose")]
    [InlineData("ai", "ollama", "server", "src")]
    public async Task Run_RefusesADirectoryThatDeclaresNoComponent(params string[] path) =>
        (await Resolve(path)).Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("declares no component");

    [Fact]
    public async Task Run_RefusesAComponentOutsideAnyCheckout()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns((IDirectory?)null);

        (await Resolve("ai", "ollama", "server")).Outcome.IsFailure.ShouldBeTrue();
    }
}
