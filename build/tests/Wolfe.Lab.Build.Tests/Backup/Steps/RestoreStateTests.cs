using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Backup.Services;
using Wolfe.Lab.Build.Backup.Steps;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Restic;

namespace Wolfe.Lab.Build.Tests.Backup.Steps;

public class RestoreStateTests
{
    private static readonly IDirectory State = new PhysicalDirectory("/Users/lab/Docker/forgejo/data");
    private static readonly ResticRepository Repository = new(new Dictionary<string, string> { ["RESTIC_REPOSITORY"] = "/Volumes/Data2/restic" });
    private static readonly RestorePoint Point = new(new ResticSnapshot("0ff3ec5c", DateTimeOffset.UnixEpoch, ["service:forgejo"], [State.AbsolutePath]));
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly IRestic _restic = Substitute.For<IRestic>();
    private readonly IStateDirectories _directories = Substitute.For<IStateDirectories>();
    private readonly Slice _slice = new("forgejo", new PhysicalDirectory("/lab/forgejo"), new PhysicalDirectory("/lab/release/forgejo"));

    public RestoreStateTests() => _directories.Move(Arg.Any<IDirectory>(), Arg.Any<IDirectory>()).Returns(true);

    private RestoreState Step(string? container) =>
        new(_docker, _restic, _directories, new BackupPlan([State], [], container, container, []), Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_StopsSetsAsideRestoresToTheRootAndStarts()
    {
        var result = await Step("forgejo").Run(_slice, Repository, Point, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        Received.InOrder(async () =>
        {
            await _docker.ComposeStop(_slice.Release, Arg.Any<CancellationToken>());
            _directories.Move(Arg.Is<IDirectory>(d => d.AbsolutePath == State.AbsolutePath), Arg.Is<IDirectory>(d => d.AbsolutePath.StartsWith(State.AbsolutePath + ".bak-")));
            await _restic.Restore(Repository, "0ff3ec5c", Arg.Is<IDirectory>(d => d.AbsolutePath == Path.GetFullPath(RestoreState.Root)), Arg.Is<IReadOnlyList<string>>(i => i.Count == 0), Arg.Any<CancellationToken>());
            await _docker.ComposeStart(_slice.Release, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Run_PutsTheLiveStateBackWhenTheRestoreFails()
    {
        _restic.Restore(Repository, "0ff3ec5c", Arg.Any<IDirectory>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("restic failed"));

        await Should.ThrowAsync<InvalidOperationException>(() => Step("forgejo").Run(_slice, Repository, Point, TestContext.Current.CancellationToken));

        _directories.Received().Move(Arg.Is<IDirectory>(d => d.AbsolutePath.StartsWith(State.AbsolutePath + ".bak-")), Arg.Is<IDirectory>(d => d.AbsolutePath == State.AbsolutePath));
        await _docker.Received().ComposeStart(_slice.Release, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_LeavesDockerAloneForAWarmSlice()
    {
        var result = await Step(container: null).Run(_slice, Repository, Point, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _docker.ReceivedCalls().ShouldBeEmpty();
    }
}
