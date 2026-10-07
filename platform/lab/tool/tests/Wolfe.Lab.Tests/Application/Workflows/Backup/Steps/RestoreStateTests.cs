using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Backup;
using Wolfe.Lab.Application.Workflows.Backup.Models;
using Wolfe.Lab.Application.Workflows.Backup.Steps;
using Wolfe.Lab.Infrastructure.Restic;

namespace Wolfe.Lab.Tests.Application.Workflows.Backup.Steps;

public class RestoreStateTests
{
    private static readonly IDirectory State = new PhysicalDirectory("/Users/lab/Docker/forgejo/data");
    private static readonly ResticRepository Repository = new(new Dictionary<string, string> { ["RESTIC_REPOSITORY"] = "/Volumes/Data2/restic" });
    private static readonly RestorePoint Point = new(new ResticSnapshot("0ff3ec5c", DateTimeOffset.UnixEpoch, ["service:forgejo"], [State.AbsolutePath]));
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly IRestic _restic = Substitute.For<IRestic>();
    private readonly IStateDirectories _directories = Substitute.For<IStateDirectories>();
    private static readonly IDirectory Stack = new PhysicalDirectory("/lab/root/forgejo-server");

    public RestoreStateTests() => _directories.Move(Arg.Any<IDirectory>(), Arg.Any<IDirectory>()).Returns(true);

    private RestoreState Step() => new(_docker, _restic, _directories, Substitute.For<IWorkflowLog>());

    private static BackupPlan Plan(string? container) => new("forgejo", [State], [], [], container is null ? null : Stack, container, container);

    [Fact]
    public async Task Run_StopsSetsAsideRestoresToTheRootAndStarts()
    {
        var result = await Step().Run(Plan("forgejo"), Repository, Point, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        Received.InOrder(async () =>
        {
            await _docker.ComposeStop(Stack, Arg.Any<CancellationToken>());
            _directories.Move(Arg.Is<IDirectory>(d => d.AbsolutePath == State.AbsolutePath), Arg.Is<IDirectory>(d => d.AbsolutePath.StartsWith(State.AbsolutePath + ".bak-")));
            await _restic.Restore(Repository, "0ff3ec5c", Arg.Is<IDirectory>(d => d.AbsolutePath == Path.GetFullPath(RestoreState.Root)), Arg.Is<IReadOnlyList<string>>(i => i.Count == 0), Arg.Any<CancellationToken>());
            await _docker.ComposeStart(Stack, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Run_PutsTheLiveStateBackWhenTheRestoreFails()
    {
        _restic.Restore(Repository, "0ff3ec5c", Arg.Any<IDirectory>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("restic failed"));

        await Should.ThrowAsync<InvalidOperationException>(() => Step().Run(Plan("forgejo"), Repository, Point, TestContext.Current.CancellationToken));

        _directories.Received().Move(Arg.Is<IDirectory>(d => d.AbsolutePath.StartsWith(State.AbsolutePath + ".bak-")), Arg.Is<IDirectory>(d => d.AbsolutePath == State.AbsolutePath));
        await _docker.Received().ComposeStart(Stack, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_LeavesDockerAloneForAWarmSnapshot()
    {
        var result = await Step().Run(Plan(null), Repository, Point, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _docker.ReceivedCalls().ShouldBeEmpty();
    }
}
