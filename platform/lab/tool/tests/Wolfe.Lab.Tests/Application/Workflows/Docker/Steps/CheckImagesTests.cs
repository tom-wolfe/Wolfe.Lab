using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Docker.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Images;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Docker.Steps;

public class CheckImagesTests : IDisposable
{
    private readonly DirectoryInfo _component = Directory.CreateTempSubdirectory("lab-image-");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public CheckImagesTests()
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(_component.FullName));
        File.WriteAllText(Path.Combine(_component.FullName, "Dockerfile"), "FROM scratch");
    }

    public void Dispose() => _component.Delete(recursive: true);

    private StepResult Check(string context = ".", string? dockerfile = null)
    {
        var image = ImageComponent.Create(new DocumentSource(RepositoryPath.From("platform/ci/image/component.yaml")), ComponentName.From("image"), ComponentKind.Package,
            ImageTag.From("code.twolfe.dev/tom-wolfe/ci")).Value.ShouldNotBeNull();
        image.Context = context;
        image.Dockerfile = dockerfile ?? image.Dockerfile;
        return new CheckImages(_fileSystem, Substitute.For<IWorkflowLog>()).Run(Catalogs.UnitOf<ImageComponent>(image));
    }

    [Fact]
    public void Run_PassesAnImageWithItsDockerfile() =>
        Check().IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_FailsAContextWithoutADockerfile()
    {
        Directory.CreateDirectory(Path.Combine(_component.FullName, "empty"));

        var result = Check("empty");

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("no Dockerfile in 'empty'");
    }

    [Fact]
    public void Run_FindsADockerfileBelowAWiderContext()
    {
        // The CI image: the repository root as its context, for the tool manifest, and its
        // Dockerfile down in the component.
        var root = _component.CreateSubdirectory("root");
        File.WriteAllText(Path.Combine(root.CreateSubdirectory("ci").CreateSubdirectory("image").FullName, "Dockerfile"), "FROM scratch");

        Check("root", "ci/image/Dockerfile").IsFailure.ShouldBeFalse();
    }
}
