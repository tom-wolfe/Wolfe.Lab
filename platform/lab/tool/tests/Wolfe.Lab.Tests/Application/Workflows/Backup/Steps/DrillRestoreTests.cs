using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Backup.Models;
using Wolfe.Lab.Application.Workflows.Backup.Steps;
using Wolfe.Lab.Infrastructure.Restic;

namespace Wolfe.Lab.Tests.Application.Workflows.Backup.Steps;

public class DrillRestoreTests : IDisposable
{
    private static readonly ResticRepository Repository = new(new Dictionary<string, string> { ["RESTIC_REPOSITORY"] = "/Volumes/Data2/restic" });
    private static readonly ResticSnapshot Latest = new("0ff3ec5c", DateTimeOffset.UnixEpoch, ["service:forgejo"], []);
    private readonly DirectoryInfo _scratch = Directory.CreateTempSubdirectory("lab-proof-");
    private readonly IRestic _restic = Substitute.For<IRestic>();

    public void Dispose() => _scratch.Delete(recursive: true);

    private DrillRestore Step(bool dryRun = false) =>
        new(_restic, ScratchFileSystem.Create(), new WorkflowJob("forgejo", "restore-drill", dryRun), Substitute.For<IWorkflowLog>());

    private static BackupPlan Plan(IReadOnlyList<string> verify) =>
        new("forgejo", [new PhysicalDirectory("/Users/lab/Docker/forgejo/data")], [], verify, new PhysicalDirectory("/lab/root/forgejo-server"), "forgejo", "forgejo");

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

        var result = await Step().Run(Plan([proof]), Repository, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _restic.Received().Restore(Repository, "0ff3ec5c", Arg.Any<IDirectory>(), Arg.Is<IReadOnlyList<string>>(i => i.Single() == proof), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_NamesEachPathThatDidNotComeBack()
    {
        _restic.FindSnapshot(Repository, "service:forgejo", null, Arg.Any<CancellationToken>()).Returns(Latest);

        var result = await Step().Run(Plan(["/nowhere/forgejo.db", "/nowhere/app.ini"]), Repository, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().Count.ShouldBe(2);
    }

    [Fact]
    public async Task Run_FailsWithoutASnapshotToDrill()
    {
        _restic.FindSnapshot(Repository, "service:forgejo", null, Arg.Any<CancellationToken>()).Returns((ResticSnapshot?)null);

        var result = await Step().Run(Plan(["/nowhere/forgejo.db"]), Repository, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        await _restic.DidNotReceiveWithAnyArgs().Restore(default!, default!, default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_AssertsNothingOnARehearsal()
    {
        _restic.FindSnapshot(Repository, "service:forgejo", null, Arg.Any<CancellationToken>()).Returns(Latest);

        var result = await Step(dryRun: true).Run(Plan(["/nowhere/forgejo.db"]), Repository, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
    }
}
