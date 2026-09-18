using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Restic;

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
}
