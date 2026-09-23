using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Workflows.Docker.Models;
using Wolfe.Lab.Build.Workflows.Docker.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Docker.Steps;

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

    private CheckImages Step(bool pushed, params DockerImage[] images) =>
        new(new ComponentImages(images, pushed), _fileSystem, Substitute.For<IWorkflowLog>());

    [Fact]
    public void Run_PassesAPushedImageWhoseTagNamesItsRegistry() =>
        Step(pushed: true, new DockerImage("code.twolfe.dev/tom-wolfe/ci", ".")).Run().IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_PassesABareTagThatStaysOnTheNode() =>
        Step(pushed: false, new DockerImage("lab/mail-watcher", ".")).Run().IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_FailsAPushedTagThatWouldMeanDockerHub()
    {
        var result = Step(pushed: true, new DockerImage("lab/ci", ".")).Run();

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("Docker Hub");
    }

    [Fact]
    public void Run_FailsAContextWithoutADockerfile()
    {
        Directory.CreateDirectory(Path.Combine(_component.FullName, "empty"));

        var result = Step(pushed: false, new DockerImage("lab/thing", "empty")).Run();

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("no Dockerfile in 'empty'");
    }
}
