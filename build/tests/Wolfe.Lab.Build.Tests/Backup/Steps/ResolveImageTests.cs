using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Backup.Steps;

namespace Wolfe.Lab.Build.Tests.Backup.Steps;

public class ResolveImageTests
{
    private readonly IDocker _docker = Substitute.For<IDocker>();

    private ResolveImage Step(string? container) =>
        new(_docker, new BackupPlan([new PhysicalDirectory("/Volumes/Data2/files")], [], container, container, []), Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_PairsNothingWithAWarmSnapshot()
    {
        var result = await Step(container: null).Run(TestContext.Current.CancellationToken);

        result.Value.ShouldBe(SnapshotImage.None);
        _docker.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_TagsAWarmSnapshotWithTheImageNamedForIt()
    {
        _docker.Inspect("immich-server", Arg.Any<CancellationToken>()).Returns(new ContainerState("ghcr.io/immich-app/immich-server:v3.2.2", true));
        var step = new ResolveImage(_docker, new BackupPlan([new PhysicalDirectory("/Volumes/Data2/immich")], [], null, "immich-server", []), Substitute.For<IWorkflowLog>());

        var result = await step.Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Tag.ShouldBe("ghcr.io/immich-app/immich-server:v3.2.2");
    }

    [Fact]
    public async Task Run_ReadsTheImageTheContainerRuns()
    {
        _docker.Inspect("jellyfin", Arg.Any<CancellationToken>()).Returns(new ContainerState("jellyfin/jellyfin:12.0", true));

        var result = await Step("jellyfin").Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Tag.ShouldBe("jellyfin/jellyfin:12.0");
    }
}
