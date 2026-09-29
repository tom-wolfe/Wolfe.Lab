using Ritten.Reporting;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Clients.Releases.Steps;

namespace Wolfe.Lab.Build.Tests.Clients.Releases.Steps;

public class PublishArtifactsTests : IDisposable
{
    private readonly DirectoryInfo _node = Directory.CreateTempSubdirectory("lab-published-");

    public void Dispose() => _node.Delete(recursive: true);

    private static readonly DateTime Old = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private Artifact Output(string name)
    {
        var output = _node.CreateSubdirectory(name);
        Directory.SetLastWriteTimeUtc(output.FullName, Old);
        return new Artifact(new PhysicalDirectory(_node.FullName), new PhysicalDirectory(output.FullName));
    }

    [Fact]
    public void Stamp_IsTheNewestWriteUnderAnyOutput()
    {
        var alloy = Output("alloy");
        var nested = Directory.CreateDirectory(Path.Combine(alloy.Output.AbsolutePath, "modules")).FullName;
        File.WriteAllText(Path.Combine(nested, "a.alloy"), "");
        var newest = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(Path.Combine(nested, "a.alloy"), newest);
        Directory.SetLastWriteTimeUtc(nested, Old);
        Directory.SetLastWriteTimeUtc(alloy.Output.AbsolutePath, Old);

        PublishArtifacts.Stamp([alloy, Output("other")]).ShouldBe(new DateTimeOffset(newest));
    }

    [Fact]
    public void Stamp_CountsADirectoryWhoseEntriesChanged()
    {
        // A deleted file leaves nothing behind but its directory's time.
        var alloy = Output("alloy");
        var removed = new DateTime(2026, 9, 29, 13, 0, 0, DateTimeKind.Utc);
        Directory.SetLastWriteTimeUtc(alloy.Output.AbsolutePath, removed);

        PublishArtifacts.Stamp([alloy]).ShouldBe(new DateTimeOffset(removed));
    }

    [Fact]
    public void Stamp_IsNullWithNothingPublished() =>
        PublishArtifacts.Stamp([]).ShouldBeNull();
}

public class PublishArtifactsReportTests
{
    private readonly IReleaseInstaller _installer = Substitute.For<IReleaseInstaller>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ReportSection _section = new(PublishArtifacts.Section);
    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();

    public PublishArtifactsReportTests()
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory("/repo/monitoring/alloy/agent"));
        _report.Section(PublishArtifacts.Section).Returns(_section);
    }

    private static readonly Artifact Alloy = new(
        new PhysicalDirectory("/repo/monitoring/alloy/agent/config"), new PhysicalDirectory("/nowhere/alloy"));

    private Task<StepResult<PublishedArtifacts>> Publish(bool dryRun = false) =>
        new PublishArtifacts(_installer, _fileSystem, _report, new WorkflowJob("agents", "deploy", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(new Artifacts([Alloy]), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Run_ReportsWhatItPublishedAndTheFilesThatChanged()
    {
        _installer.Install(Arg.Any<IDirectory>(), Arg.Any<IDirectory>(), Arg.Any<CancellationToken>()).Returns(["+ config.alloy", "- old.alloy"]);

        await Publish();

        var summary = _section.Entries[0].ShouldBeOfType<ReportParagraph>();
        summary.Tone.ShouldBe(ReportTone.Success);
        summary.Markdown.ShouldBe("Published `config` to `/nowhere/alloy`: 2 files changed.");
        _section.Entries[1].ShouldBeOfType<ReportDetailsBlock>().Markdown.ShouldContain("+ config.alloy\n- old.alloy");
    }

    [Fact]
    public async Task Run_ReportsAnArtifactThatAlreadyMatched()
    {
        _installer.Install(Arg.Any<IDirectory>(), Arg.Any<IDirectory>(), Arg.Any<CancellationToken>()).Returns([]);

        await Publish();

        _section.Entries.ShouldHaveSingleItem().ShouldBeOfType<ReportParagraph>().Markdown.ShouldContain("already matched");
    }

    [Fact]
    public async Task Run_ReportsARehearsalAsWhatItWouldDo()
    {
        _installer.Install(Arg.Any<IDirectory>(), Arg.Any<IDirectory>(), Arg.Any<CancellationToken>()).Returns(["~ config.alloy"]);

        await Publish(dryRun: true);

        var summary = _section.Entries[0].ShouldBeOfType<ReportParagraph>();
        summary.Tone.ShouldBe(ReportTone.Note);
        summary.Markdown.ShouldBe("Would publish `config` to `/nowhere/alloy`: 1 file to change.");
    }
}
