using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Clients.Releases.Steps;

namespace Wolfe.Lab.Tests.Clients.Releases.Steps;

public class ResolveComponentTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-placement-");
    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public ResolveComponentTests() =>
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory(_checkout.FullName));

    public void Dispose() => _checkout.Delete(recursive: true);

    private Task<StepResult<Wolfe.Lab.Values.Component>> Resolve(params string[] path)
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(Directory.CreateDirectory(Path.Combine([_checkout.FullName, .. path])).FullName));
        return new ResolveComponent(_git, _fileSystem, Substitute.For<IWorkflowLog>()).Run(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_ReadsAreaServiceAndComponentFromThePath()
    {
        var placement = (await Resolve("ai", "ollama", "server")).Value.ShouldNotBeNull();

        placement.Area.ShouldBe("ai");
        placement.Service.ShouldBe("ollama");
        placement.Name.ShouldBe("server");
        placement.QualifiedName.ShouldBe("ai-ollama-server");
        placement.ResourceAttributes.ShouldBe("lab.area=ai,lab.service=ollama,lab.component=server");
    }

    [Theory]
    [InlineData("loose", "compose")]
    [InlineData("a", "b", "c", "d")]
    public async Task Run_RefusesAComponentThatIsNotAreaServiceComponent(params string[] path) =>
        (await Resolve(path)).Outcome.IsFailure.ShouldBeTrue();

    [Fact]
    public async Task Run_RefusesAComponentOutsideAnyCheckout()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns((IDirectory?)null);

        (await Resolve("ai", "ollama", "server")).Outcome.IsFailure.ShouldBeTrue();
    }
}
