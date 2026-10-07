using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Releases;

public class ResolveArtifactsTests : IDisposable
{
    private readonly DirectoryInfo _component = Directory.CreateTempSubdirectory("lab-component-");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IOptions<LabDirectories> _roots = Options.Create(new LabDirectories { Root = new PhysicalDirectory("/lab/root") });

    public ResolveArtifactsTests()
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(_component.FullName));
        _component.CreateSubdirectory("config");
    }

    public void Dispose() => _component.Delete(recursive: true);

    private StepResult<Artifacts> Resolve(params ArtifactOptions[] artifacts) =>
        new ResolveArtifacts(new ArtifactDeclarations(artifacts), _fileSystem, _roots, Substitute.For<IWorkflowLog>()).Run(unit: null);

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

    [Fact]
    public void Run_InstallsTheDeploymentFirst_UnderItsName()
    {
        var unit = Catalogs.Unit("personal/immich/compose", Catalogs.Compose("server", "immich-server"));

        var artifacts = new ResolveArtifacts(new ArtifactDeclarations([new ArtifactOptions { Source = "config", Output = "${LAB_ROOT}/immich-config" }], InstallsUnit: true),
            _fileSystem, _roots, Substitute.For<IWorkflowLog>()).Run(unit).Value.ShouldNotBeNull().Items;

        artifacts.Select(artifact => (artifact.Source.AbsolutePath, artifact.Output.AbsolutePath)).ShouldBe([
            (_component.FullName, "/lab/root/immich-server"),
            (Path.Combine(_component.FullName, "config"), "/lab/root/immich-config")
        ]);
    }

    [Fact]
    public void Run_RefusesToInstallADeploymentNothingResolved() =>
        new ResolveArtifacts(new ArtifactDeclarations([], InstallsUnit: true), _fileSystem, _roots, Substitute.For<IWorkflowLog>()).Run(unit: null)
            .Outcome.IsFailure.ShouldBeTrue();
}
