using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Restic;
using Wolfe.Lab.Build.Restic.Models;

namespace Wolfe.Lab.Build.Tests.Restic;

public class ResticClientTests
{
    private static readonly ResticRepository Repository = new(new Dictionary<string, string>
    {
        ["RESTIC_REPOSITORY"] = "/Volumes/Data2/restic",
        ["RESTIC_PASSWORD"] = "s3cret"
    });

    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    [Fact]
    public async Task Backup_KeepsTheRepositoryInTheEnvironmentAndReadsTheSnapshotBack()
    {
        Command? ran = null;
        _commands.Run(Arg.Do<Command>(c => ran = c), Arg.Any<CancellationToken>())
            .Returns(new CommandResult(0, "using parent snapshot 6aa9c4fb\n\nsnapshot 0ff3ec5c saved\n", ""));

        var snapshot = await new ResticClient(_commands).Backup(
            Repository,
            [new PhysicalDirectory("/Volumes/Data2/files")],
            ["/Volumes/Data2/files/.cache"],
            ["service:files"],
            TestContext.Current.CancellationToken);

        snapshot.Id.ShouldBe("0ff3ec5c");
        var command = ran.ShouldNotBeNull();
        command.Path.ShouldBe("restic");
        command.Arguments.ShouldBe(["backup", Path.GetFullPath("/Volumes/Data2/files"), "--exclude", "/Volumes/Data2/files/.cache", "--tag", "service:files"]);
        command.EnvironmentVariables["RESTIC_PASSWORD"].ShouldBe("s3cret");
        command.Arguments.ShouldNotContain("s3cret");
    }

    [Fact]
    public async Task Backup_RefusesACleanExitThatSavedNothing()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "", ""));

        await Should.ThrowAsync<CommandFailedException>(() =>
            new ResticClient(_commands).Backup(Repository, [new PhysicalDirectory("/x")], [], [], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Forget_NamesTheSnapshot()
    {
        Command? ran = null;
        _commands.Run(Arg.Do<Command>(c => ran = c), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "", ""));

        await new ResticClient(_commands).Forget(Repository, new Snapshot("0ff3ec5c"), TestContext.Current.CancellationToken);

        ran.ShouldNotBeNull().Arguments.ShouldBe(["forget", "0ff3ec5c"]);
    }

    [Fact]
    public void BackupCommand_RehearsesWithResticsOwnDryRun()
    {
        var command = ResticClient.BackupCommand(Repository, [new PhysicalDirectory("/x")], [], ["service:files"], dryRun: true);

        command.Arguments.Take(3).ShouldBe(["backup", "--dry-run", "--verbose"]);
    }

    [Fact]
    public async Task Copy_TakesEverythingFromTheOffsiteEnvironment()
    {
        Command? ran = null;
        _commands.Run(Arg.Do<Command>(c => ran = c), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "", ""));
        var offsite = new OffsiteRepository(new ResticRepository(new Dictionary<string, string>
        {
            ["RESTIC_REPOSITORY"] = "s3:https://s3.eu.backblazeb2.com/wolfe-lab-restic",
            ["RESTIC_FROM_REPOSITORY"] = "/Volumes/Data2/restic"
        }));

        await new ResticClient(_commands).Copy(offsite, TestContext.Current.CancellationToken);

        var command = ran.ShouldNotBeNull();
        command.Arguments.ShouldBe(["copy"]);
        command.EnvironmentVariables["RESTIC_FROM_REPOSITORY"].ShouldBe("/Volumes/Data2/restic");
    }

    [Fact]
    public async Task Prune_SpellsThePolicyAndPrunesInTheSamePass()
    {
        Command? ran = null;
        _commands.Run(Arg.Do<Command>(c => ran = c), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "", ""));

        await new ResticClient(_commands).Prune(Repository, new RetentionPolicy(7, 5, 12, ["pre-upgrade"]), TestContext.Current.CancellationToken);

        ran.ShouldNotBeNull().Arguments.ShouldBe(["forget", "--keep-daily", "7", "--keep-weekly", "5", "--keep-monthly", "12", "--keep-tag", "pre-upgrade", "--prune"]);
    }

    [Fact]
    public void PruneCommand_RehearsesWithResticsOwnDryRun()
    {
        var command = ResticClient.PruneCommand(Repository, new RetentionPolicy(7, 5, 12, []), dryRun: true);

        command.Arguments.ShouldNotContain("--prune");
        command.Arguments.Last().ShouldBe("--dry-run");
    }

    [Fact]
    public async Task Check_ReadsASampleOnlyWhenAsked()
    {
        var ran = new List<Command>();
        _commands.Run(Arg.Do<Command>(ran.Add), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "", ""));

        await new ResticClient(_commands).Check(Repository, null, TestContext.Current.CancellationToken);
        await new ResticClient(_commands).Check(Repository, "5%", TestContext.Current.CancellationToken);

        ran[0].Arguments.ShouldBe(["check"]);
        ran[1].Arguments.ShouldBe(["check", "--read-data-subset=5%"]);
    }
}
