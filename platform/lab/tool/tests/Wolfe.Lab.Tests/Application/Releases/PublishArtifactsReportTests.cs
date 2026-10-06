using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Tests.Application.Releases;

public class PublishArtifactsReportTests
{
    private readonly IReleaseInstaller _installer = Substitute.For<IReleaseInstaller>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ReportSection _section = new(ReportSections.Artifacts);
    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();

    public PublishArtifactsReportTests()
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory("/repo/monitoring/alloy/agent"));
        _report.Section(ReportSections.Artifacts).Returns(_section);
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
