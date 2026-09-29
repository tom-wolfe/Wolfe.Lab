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
}
