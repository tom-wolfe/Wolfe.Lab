using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Backup.Steps;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Restic;

namespace Wolfe.Lab.Build.Tests.Backup.Steps;

public class TakeSnapshotTests
{
    private static readonly IDirectory State = new PhysicalDirectory("/Users/lab/Docker/jellyfin");
    private static readonly ResticRepository Repository = new(new Dictionary<string, string> { ["RESTIC_REPOSITORY"] = "/Volumes/Data2/restic" });
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly IRestic _restic = Substitute.For<IRestic>();
    private readonly Slice _slice = new("jellyfin", new PhysicalDirectory("/lab/jellyfin"), new PhysicalDirectory("/Users/lab/.local/share/Wolfe.Lab/jellyfin"));

    public TakeSnapshotTests() =>
        _restic.Backup(Repository, Arg.Any<IReadOnlyList<IDirectory>>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Snapshot("0ff3ec5c"));

    private TakeSnapshot Step(string? container, bool dryRun = false) =>
        new(_docker, _restic, new BackupPlan([State], ["/Users/lab/Docker/jellyfin/cache"], container, container, []), new WorkflowJob("jellyfin", "backup", dryRun), Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_SnapshotsWarmWithoutTouchingDocker()
    {
        var result = await Step(container: null).Run(_slice, Repository, SnapshotImage.None, TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Id.ShouldBe("0ff3ec5c");
        _docker.ReceivedCalls().ShouldBeEmpty();
        await _restic.Received().Backup(Repository, Arg.Is<IReadOnlyList<IDirectory>>(p => p.Single() == State), Arg.Is<IReadOnlyList<string>>(e => e.Single() == "/Users/lab/Docker/jellyfin/cache"), Arg.Is<IReadOnlyList<string>>(t => t.SequenceEqual(new[] { "service:jellyfin" })), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_StopsBeforeTheSnapshotAndStartsAfterIt()
    {
        _docker.Inspect("jellyfin", Arg.Any<CancellationToken>()).Returns(new ContainerState("jellyfin/jellyfin:12.0", false));

        var result = await Step("jellyfin").Run(_slice, Repository, new SnapshotImage("jellyfin/jellyfin:12.0"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        Received.InOrder(async () =>
        {
            await _docker.ComposeStop(_slice.Release, Arg.Any<CancellationToken>());
            await _restic.Backup(Repository, Arg.Any<IReadOnlyList<IDirectory>>(), Arg.Any<IReadOnlyList<string>>(), Arg.Is<IReadOnlyList<string>>(t => t.Contains("image:jellyfin/jellyfin:12.0")), Arg.Any<CancellationToken>());
            await _docker.ComposeStart(_slice.Release, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Run_StartsTheStackEvenWhenResticFails()
    {
        _restic.Backup(Repository, Arg.Any<IReadOnlyList<IDirectory>>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns<Snapshot>(_ => throw new InvalidOperationException("restic failed"));

        await Should.ThrowAsync<InvalidOperationException>(() => Step("jellyfin").Run(_slice, Repository, SnapshotImage.None, TestContext.Current.CancellationToken));

        await _docker.Received().ComposeStart(_slice.Release, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_DiscardsASnapshotTakenUnderARestartedStack()
    {
        _docker.Inspect("jellyfin", Arg.Any<CancellationToken>()).Returns(new ContainerState("jellyfin/jellyfin:12.0", true));

        var result = await Step("jellyfin").Run(_slice, Repository, SnapshotImage.None, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        await _restic.Received().Forget(Repository, new Snapshot("0ff3ec5c"), Arg.Any<CancellationToken>());
        await _docker.Received().ComposeStart(_slice.Release, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_DoesNotJudgeARehearsalByARunningStack()
    {
        var result = await Step("jellyfin", dryRun: true).Run(_slice, Repository, SnapshotImage.None, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        await _docker.DidNotReceiveWithAnyArgs().Inspect(default!, TestContext.Current.CancellationToken);
        await _restic.DidNotReceiveWithAnyArgs().Forget(default!, default!, TestContext.Current.CancellationToken);
    }
}
