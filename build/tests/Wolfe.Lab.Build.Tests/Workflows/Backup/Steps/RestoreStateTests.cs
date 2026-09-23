using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Workflows.Backup;
using Wolfe.Lab.Build.Workflows.Backup.Models;
using Wolfe.Lab.Build.Workflows.Backup.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Backup.Steps;

public class RestoreStateTests
{
    private static readonly IDirectory State = new PhysicalDirectory("/Users/lab/Docker/forgejo/data");
    private static readonly ResticRepository Repository = new(new Dictionary<string, string> { ["RESTIC_REPOSITORY"] = "/Volumes/Data2/restic" });
    private static readonly RestorePoint Point = new(new ResticSnapshot("0ff3ec5c", DateTimeOffset.UnixEpoch, ["service:forgejo"], [State.AbsolutePath]));
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly IRestic _restic = Substitute.For<IRestic>();
    private readonly IStateDirectories _directories = Substitute.For<IStateDirectories>();
    private readonly Release _release = new("forgejo", new PhysicalDirectory("/lab/release/forgejo"));

    public RestoreStateTests() => _directories.Move(Arg.Any<IDirectory>(), Arg.Any<IDirectory>()).Returns(true);

    private RestoreState Step(string? container) =>
        new(_docker, _restic, _directories, new BackupPlan([State], [], container, container, []), Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_StopsSetsAsideRestoresToTheRootAndStarts()
    {
        var result = await Step("forgejo").Run(_release, Repository, Point, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        Received.InOrder(async () =>
        {
            await _docker.ComposeStop(_release.Directory, Arg.Any<CancellationToken>());
            _directories.Move(Arg.Is<IDirectory>(d => d.AbsolutePath == State.AbsolutePath), Arg.Is<IDirectory>(d => d.AbsolutePath.StartsWith(State.AbsolutePath + ".bak-")));
            await _restic.Restore(Repository, "0ff3ec5c", Arg.Is<IDirectory>(d => d.AbsolutePath == Path.GetFullPath(RestoreState.Root)), Arg.Is<IReadOnlyList<string>>(i => i.Count == 0), Arg.Any<CancellationToken>());
            await _docker.ComposeStart(_release.Directory, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Run_PutsTheLiveStateBackWhenTheRestoreFails()
    {
        _restic.Restore(Repository, "0ff3ec5c", Arg.Any<IDirectory>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("restic failed"));

        await Should.ThrowAsync<InvalidOperationException>(() => Step("forgejo").Run(_release, Repository, Point, TestContext.Current.CancellationToken));

        _directories.Received().Move(Arg.Is<IDirectory>(d => d.AbsolutePath.StartsWith(State.AbsolutePath + ".bak-")), Arg.Is<IDirectory>(d => d.AbsolutePath == State.AbsolutePath));
        await _docker.Received().ComposeStart(_release.Directory, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_LeavesDockerAloneForAWarmSnapshot()
    {
        var result = await Step(container: null).Run(_release, Repository, Point, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _docker.ReceivedCalls().ShouldBeEmpty();
    }
}
