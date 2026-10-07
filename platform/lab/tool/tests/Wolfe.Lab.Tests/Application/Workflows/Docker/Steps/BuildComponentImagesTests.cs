using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Docker.Steps;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Docker.Steps;

public class BuildComponentImagesTests : IDisposable
{
    private readonly DirectoryInfo _component = Directory.CreateTempSubdirectory("lab-watcher-");
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public BuildComponentImagesTests() => _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(_component.FullName));

    public void Dispose() => _component.Delete(recursive: true);

    private Task<StepResult> Build(WorkflowName workflow) =>
        new BuildComponentImages(_docker, _fileSystem, Substitute.For<IWorkflowLog>())
            .Run(Catalogs.Unit("personal/mail/watcher", Catalogs.Docker("watcher", "watcher") with { Workflow = workflow }), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Run_BuildsTheComponentsImageFromItsDirectory()
    {
        File.WriteAllText(Path.Combine(_component.FullName, BuildComponentImages.Dockerfile), "FROM scratch");

        (await Build(WorkflowName.DotNetService)).IsFailure.ShouldBeFalse();

        await _docker.Received().Build(Arg.Is<IDirectory>(context => context.AbsolutePath == _component.FullName), "lab/mail-watcher", Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_RefusesAComponentWithNoDockerfile() =>
        (await Build(WorkflowName.DotNetService)).IsFailure.ShouldBeTrue();

    [Fact]
    public async Task Run_BuildsNothingForAStackOfPublishedImages()
    {
        (await Build(WorkflowName.Docker)).IsFailure.ShouldBeFalse();

        _docker.ReceivedCalls().ShouldBeEmpty();
    }
}
