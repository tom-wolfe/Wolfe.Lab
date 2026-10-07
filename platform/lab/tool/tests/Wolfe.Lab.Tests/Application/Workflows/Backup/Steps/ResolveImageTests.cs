using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Backup.Models;
using Wolfe.Lab.Application.Workflows.Backup.Steps;

namespace Wolfe.Lab.Tests.Application.Workflows.Backup.Steps;

public class ResolveImageTests
{
    private readonly IDocker _docker = Substitute.For<IDocker>();

    private ResolveImage Step() => new(_docker, Substitute.For<IWorkflowLog>());

    private static BackupPlan Plan(string? container) => new("files", [new PhysicalDirectory("/Volumes/Data2/files")], [], [], null, container, container);

    [Fact]
    public async Task Run_PairsNothingWithAWarmSnapshot()
    {
        var result = await Step().Run(Plan(null), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(SnapshotImage.None);
        _docker.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_TagsAWarmSnapshotWithTheImageNamedForIt()
    {
        _docker.Inspect("immich-server", Arg.Any<CancellationToken>()).Returns(new ContainerState("ghcr.io/immich-app/immich-server:v3.2.2", true));
        var step = Step();
        var plan = new BackupPlan("immich", [new PhysicalDirectory("/Volumes/Data2/immich")], [], [], null, null, "immich-server");

        var result = await step.Run(plan, TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Tag.ShouldBe("ghcr.io/immich-app/immich-server:v3.2.2");
    }

    [Fact]
    public async Task Run_ReadsTheImageTheContainerRuns()
    {
        _docker.Inspect("jellyfin", Arg.Any<CancellationToken>()).Returns(new ContainerState("jellyfin/jellyfin:12.0", true));

        var result = await Step().Run(Plan("jellyfin"), TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Tag.ShouldBe("jellyfin/jellyfin:12.0");
    }
}
