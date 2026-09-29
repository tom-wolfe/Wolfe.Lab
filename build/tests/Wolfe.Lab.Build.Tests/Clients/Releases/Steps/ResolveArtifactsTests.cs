using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Clients.Releases.Steps;

namespace Wolfe.Lab.Build.Tests.Clients.Releases.Steps;

public class ResolveArtifactsTests : IDisposable
{
    private readonly DirectoryInfo _component = Directory.CreateTempSubdirectory("lab-component-");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly WorkflowEnvironment _environment = new(name => name == "LAB_ROOT" ? "/lab/root" : null);

    public ResolveArtifactsTests()
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(_component.FullName));
        _component.CreateSubdirectory("config");
    }

    public void Dispose() => _component.Delete(recursive: true);

    private StepResult<Artifacts> Resolve(params ArtifactOptions[] artifacts) =>
        new ResolveArtifacts(new ArtifactDeclarations(artifacts), _fileSystem, _environment, Substitute.For<IWorkflowLog>()).Run();

    private static string Message(StepResult<Artifacts> result) =>
        string.Join(" ", result.Outcome.Errors.ShouldNotBeNull().Select(error => error.Message));

    [Fact]
    public void Run_PublishesADirectoryOfTheComponentIntoTheRoot()
    {
        var artifact = Resolve(new ArtifactOptions { Source = "config", Output = "${LAB_ROOT}/alloy" }).Value.ShouldNotBeNull().Items.ShouldHaveSingleItem();

        artifact.Source.AbsolutePath.ShouldBe(Path.Combine(_component.FullName, "config"));
        artifact.Output.AbsolutePath.ShouldBe("/lab/root/alloy");
    }

    [Fact]
    public void Run_RefusesAnOutputOutsideTheRoot() =>
        Message(Resolve(new ArtifactOptions { Source = "config", Output = "/Users/tomwolfe/.config/alloy" })).ShouldContain("not inside /lab/root");

    [Fact]
    public void Run_RefusesTheRootItself() =>
        Message(Resolve(new ArtifactOptions { Source = "config", Output = "${LAB_ROOT}" })).ShouldContain("not inside /lab/root");

    [Fact]
    public void Run_RefusesASourceOutsideTheComponent() =>
        Message(Resolve(new ArtifactOptions { Source = "../elsewhere", Output = "${LAB_ROOT}/x" })).ShouldContain("outside the component");

    [Fact]
    public void Run_RefusesASourceThatIsNotThere() =>
        Message(Resolve(new ArtifactOptions { Source = "missing", Output = "${LAB_ROOT}/x" })).ShouldContain("not a directory");

    [Fact]
    public void Run_RefusesOutputsThatNest() =>
        Message(Resolve(
            new ArtifactOptions { Source = ".", Output = "${LAB_ROOT}/grafana" },
            new ArtifactOptions { Source = "config", Output = "${LAB_ROOT}/grafana/config" })).ShouldContain("mirror the other away");
}
