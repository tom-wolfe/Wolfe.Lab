using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Backup;
using Wolfe.Lab.Build.Backup.Steps;
using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Tests.Backup.Steps;

public class DrillRestoreTests : IDisposable
{
    private static readonly ResticRepository Repository = new(new Dictionary<string, string> { ["RESTIC_REPOSITORY"] = "/Volumes/Data2/restic" });
    private static readonly ResticSnapshot Latest = new("0ff3ec5c", DateTimeOffset.UnixEpoch, ["service:forgejo"], []);
    private readonly DirectoryInfo _scratch = Directory.CreateTempSubdirectory("lab-proof-");
    private readonly IRestic _restic = Substitute.For<IRestic>();
    private readonly Slice _slice = new("forgejo", new PhysicalDirectory("/lab/forgejo"), new PhysicalDirectory("/lab/release/forgejo"));

    public void Dispose() => _scratch.Delete(recursive: true);

    private DrillRestore Step(IReadOnlyList<string> verify, bool dryRun = false) =>
        new(_restic, new BackupPlan([new PhysicalDirectory("/Users/lab/Docker/forgejo/data")], [], "forgejo", "forgejo", verify), new WorkflowJob("forgejo", "restore-drill", dryRun), Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_PassesWhenTheProofComesBackNonEmpty()
    {
        var proof = Path.Combine(_scratch.FullName, "forgejo.db");
        _restic.FindSnapshot(Repository, "service:forgejo", null, Arg.Any<CancellationToken>()).Returns(Latest);
        _restic.Restore(Repository, "0ff3ec5c", Arg.Do<IDirectory>(target =>
        {
            var restored = Path.Join(target.AbsolutePath, proof);
            Directory.CreateDirectory(Path.GetDirectoryName(restored)!);
            File.WriteAllText(restored, "sqlite");
        }), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await Step([proof]).Run(_slice, Repository, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _restic.Received().Restore(Repository, "0ff3ec5c", Arg.Any<IDirectory>(), Arg.Is<IReadOnlyList<string>>(i => i.Single() == proof), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_NamesEachPathThatDidNotComeBack()
    {
        _restic.FindSnapshot(Repository, "service:forgejo", null, Arg.Any<CancellationToken>()).Returns(Latest);

        var result = await Step(["/nowhere/forgejo.db", "/nowhere/app.ini"]).Run(_slice, Repository, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().Count.ShouldBe(2);
    }

    [Fact]
    public async Task Run_FailsWithoutASnapshotToDrill()
    {
        _restic.FindSnapshot(Repository, "service:forgejo", null, Arg.Any<CancellationToken>()).Returns((ResticSnapshot?)null);

        var result = await Step(["/nowhere/forgejo.db"]).Run(_slice, Repository, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        await _restic.DidNotReceiveWithAnyArgs().Restore(default!, default!, default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_AssertsNothingOnARehearsal()
    {
        _restic.FindSnapshot(Repository, "service:forgejo", null, Arg.Any<CancellationToken>()).Returns(Latest);

        var result = await Step(["/nowhere/forgejo.db"], dryRun: true).Run(_slice, Repository, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
    }
}
