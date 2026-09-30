using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Releases;

namespace Wolfe.Lab.Build.Tests.Clients.Releases;

public class RsyncInstallerTests
{
    [Fact]
    public void Changes_ReadsMacOSOpenrsync() =>
        RsyncInstaller.Changes("""
            cd+++++++ ./
            >f+++++++ config.alloy
            >fcsT.... a
            .f..T.... unchanged
            *deleting sub/b
            """).ShouldBe(["+ config.alloy", "~ a", "- sub/b"]);

    [Fact]
    public void Changes_ReadsGnuRsync() =>
        RsyncInstaller.Changes("""
            cd+++++++++ modules/
            >f+++++++++ modules/a.alloy
            >fcs.T...... config.alloy
            .d..T...... ./
            *deleting   old.alloy
            """).ShouldBe(["+ modules/a.alloy", "~ config.alloy", "- old.alloy"]);

    [Fact]
    public void Changes_IsEmptyWhenNothingChanged() =>
        RsyncInstaller.Changes("").ShouldBeEmpty();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rsync_ItemisesEveryRunSoTheReportCanListTheChanges(bool dryRun) =>
        RsyncInstaller.Rsync(new PhysicalDirectory("/src"), new PhysicalDirectory("/out"), dryRun).Arguments.ShouldContain("--itemize-changes");

    [Fact]
    public void Rsync_LeavesTheCLIsOwnOutputBehind()
    {
        var arguments = RsyncInstaller.Rsync(new PhysicalDirectory("/src"), new PhysicalDirectory("/out")).Arguments;

        arguments.ShouldContain("/artifacts/");
        arguments.ShouldContain("/temp/");
        arguments.ShouldContain("ritten.json");
    }

    [Fact]
    public void Rsync_LeavesTheDeploysOwnOverrideInPlace() =>
        RsyncInstaller.Rsync(new PhysicalDirectory("/src"), new PhysicalDirectory("/out")).Arguments.ShouldContain("/compose.override.yaml");
}
