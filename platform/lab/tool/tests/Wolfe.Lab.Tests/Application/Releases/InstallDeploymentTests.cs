using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Releases;

public class InstallDeploymentTests : IDisposable
{
    private static readonly DateTime Old = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-root-");
    private readonly IReleaseInstaller _installer = Substitute.For<IReleaseInstaller>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ReportSection _section = new(ReportSections.Install);
    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();
    private readonly DeploymentUnit _unit = Catalogs.Unit("monitoring/alloy/forwarder", Catalogs.Docker("forwarder", "alloy"));

    public InstallDeploymentTests()
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory("/repo/monitoring/alloy/forwarder"));
        _report.Section(ReportSections.Install).Returns(_section);
    }

    public void Dispose() => _root.Delete(recursive: true);

    private string Installed => Path.Combine(_root.FullName, "alloy-forwarder");

    private Task<StepResult<Installation>> Install(bool dryRun = false) =>
        new InstallDeployment(_installer, _fileSystem, Options.Create(new LabDirectories { Root = new PhysicalDirectory(_root.FullName) }), _report,
                new WorkflowJob("agent", "deploy", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(_unit, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Run_MirrorsTheDeploymentUnderItsName()
    {
        _installer.Install(Arg.Any<IDirectory>(), Arg.Any<IDirectory>(), Arg.Any<CancellationToken>()).Returns([]);

        (await Install()).Value.ShouldNotBeNull().Directory.AbsolutePath.ShouldBe(Installed);

        await _installer.Received().Install(Arg.Is<IDirectory>(source => source.AbsolutePath == "/repo/monitoring/alloy/forwarder"),
            Arg.Is<IDirectory>(target => target.AbsolutePath == Installed), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_ReportsWhatItInstalledAndTheFilesThatChanged()
    {
        _installer.Install(Arg.Any<IDirectory>(), Arg.Any<IDirectory>(), Arg.Any<CancellationToken>()).Returns(["+ config/forwarder.alloy", "- old.alloy"]);

        await Install();

        var summary = _section.Entries[0].ShouldBeOfType<ReportParagraph>();
        summary.Tone.ShouldBe(ReportTone.Success);
        summary.Markdown.ShouldBe($"Installed alloy-forwarder into `{Installed}`: 2 files changed.");
        _section.Entries[1].ShouldBeOfType<ReportDetailsBlock>().Markdown.ShouldContain("+ config/forwarder.alloy\n- old.alloy");
    }

    [Fact]
    public async Task Run_ReportsAnInstallThatAlreadyMatched()
    {
        _installer.Install(Arg.Any<IDirectory>(), Arg.Any<IDirectory>(), Arg.Any<CancellationToken>()).Returns([]);

        await Install();

        _section.Entries.ShouldHaveSingleItem().ShouldBeOfType<ReportParagraph>().Markdown.ShouldContain("already matched");
    }

    [Fact]
    public async Task Run_ReportsARehearsalAsWhatItWouldDo()
    {
        _installer.Install(Arg.Any<IDirectory>(), Arg.Any<IDirectory>(), Arg.Any<CancellationToken>()).Returns(["~ config/forwarder.alloy"]);

        await Install(dryRun: true);

        var summary = _section.Entries[0].ShouldBeOfType<ReportParagraph>();
        summary.Tone.ShouldBe(ReportTone.Note);
        summary.Markdown.ShouldBe($"Would install alloy-forwarder into `{Installed}`: 1 file to change.");
    }

    [Fact]
    public void Stamp_IsTheNewestWriteUnderTheInstall()
    {
        var installed = Directory.CreateDirectory(Installed);
        var nested = installed.CreateSubdirectory("config").FullName;
        File.WriteAllText(Path.Combine(nested, "forwarder.alloy"), "");
        var newest = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(Path.Combine(nested, "forwarder.alloy"), newest);
        Directory.SetLastWriteTimeUtc(nested, Old);
        Directory.SetLastWriteTimeUtc(installed.FullName, Old);

        InstallDeployment.Stamp(new PhysicalDirectory(installed.FullName)).ShouldBe(new DateTimeOffset(newest));
    }

    [Fact]
    public void Stamp_CountsADirectoryWhoseEntriesChanged()
    {
        // A deleted file leaves nothing behind but its directory's time.
        var installed = Directory.CreateDirectory(Installed);
        var removed = new DateTime(2026, 9, 29, 13, 0, 0, DateTimeKind.Utc);
        Directory.SetLastWriteTimeUtc(installed.FullName, removed);

        InstallDeployment.Stamp(new PhysicalDirectory(installed.FullName)).ShouldBe(new DateTimeOffset(removed));
    }

    [Fact]
    public void Stamp_IsNullBeforeAnythingIsInstalled() =>
        InstallDeployment.Stamp(new PhysicalDirectory(Installed)).ShouldBeNull();
}
