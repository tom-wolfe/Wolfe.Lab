using Ritten.Engine.FileSystem;
using Wolfe.Lab.Workflows.Docker.Models;
using Wolfe.Lab.Workflows.Docker.Steps;

namespace Wolfe.Lab.Tests.Workflows.Docker.Steps;

public class CheckImagesTests : IDisposable
{
    private readonly DirectoryInfo _component = Directory.CreateTempSubdirectory("lab-image-");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public CheckImagesTests()
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(_component.FullName));
        File.WriteAllText(Path.Combine(_component.FullName, CheckImages.Dockerfile), "FROM scratch");
    }

    public void Dispose() => _component.Delete(recursive: true);

    private CheckImages Step(params ComponentImage[] images) =>
        new(new ComponentImages(images), _fileSystem, Substitute.For<IWorkflowLog>());

    [Fact]
    public void Run_PassesAnImageWhoseTagNamesItsRegistry() =>
        Step(new ComponentImage("code.twolfe.dev/tom-wolfe/ci", ".", CheckImages.Dockerfile)).Run().IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_FailsATagThatWouldMeanDockerHub()
    {
        var result = Step(new ComponentImage("lab/ci", ".", CheckImages.Dockerfile)).Run();

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("Docker Hub");
    }

    [Fact]
    public void Run_FailsAContextWithoutADockerfile()
    {
        Directory.CreateDirectory(Path.Combine(_component.FullName, "empty"));

        var result = Step(new ComponentImage("code.twolfe.dev/tom-wolfe/thing", "empty", CheckImages.Dockerfile)).Run();

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("no Dockerfile in 'empty'");
    }

    [Fact]
    public void Run_FindsADockerfileBelowAWiderContext()
    {
        // The CI image: the repository root as its context, for the tool manifest, and its
        // Dockerfile down in the component.
        var root = _component.CreateSubdirectory("root");
        var dockerfile = Path.Combine(root.CreateSubdirectory("ci").CreateSubdirectory("image").FullName, CheckImages.Dockerfile);
        File.WriteAllText(dockerfile, "FROM scratch");

        Step(new ComponentImage("code.twolfe.dev/tom-wolfe/ci", "root", "ci/image/Dockerfile")).Run().IsFailure.ShouldBeFalse();
    }

    [Theory]
    [InlineData("code.twolfe.dev/tom-wolfe/ci", "code.twolfe.dev")]
    [InlineData("code.twolfe.dev/tom-wolfe/ci:1.2", "code.twolfe.dev")]
    [InlineData("localhost:5000/ci", "localhost:5000")]
    [InlineData("lab/ci", null)]
    [InlineData("ci", null)]
    public void Registry_IsTheHostTheTagNames(string tag, string? expected) =>
        CheckImages.Registry(tag).ShouldBe(expected);
}
