using Ritten.Reporting;
using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Workflows.Docker.Models;
using Wolfe.Lab.Build.Workflows.Docker.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Docker.Steps;

public class ConvergeReleaseTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-root-");
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly ReportSection _section = new("Artifacts");
    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();
    private readonly Release _release;

    private static readonly DateTimeOffset Changed = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    public ConvergeReleaseTests()
    {
        _release = new Release("grafana", new PhysicalDirectory(_root.CreateSubdirectory("grafana").FullName));
        _report.Section(Arg.Any<string>()).Returns(_section);
    }

    public void Dispose() => _root.Delete(recursive: true);

    private string Marker => Path.Combine(_root.FullName, ".applied", "grafana");

    private Task<StepResult> Converge(DateTimeOffset? stamp, bool dryRun = false) =>
        new ConvergeRelease(_docker, new WorkflowEnvironment(name => name == "LAB_ROOT" ? _root.FullName : null),
                _report, new WorkflowJob("docker", "deploy", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(_release, new ComposeEnvironment(new Dictionary<string, string>()), new PublishedArtifacts([], stamp), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Run_TakesAFirstReleaseAsCurrentWithoutRestarting()
    {
        await Converge(Changed);

        await _docker.DidNotReceiveWithAnyArgs().ComposeStop(default!, TestContext.Current.CancellationToken);
        File.ReadAllText(Marker).ShouldBe("2026-09-29T12:00:00.0000000Z");
    }

    [Fact]
    public async Task Run_RestartsWhenTheFilesChangedSinceTheLastRestart()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Marker)!);
        File.WriteAllText(Marker, "2026-09-28T00:00:00.0000000Z");

        await Converge(Changed);

        Received.InOrder(() =>
        {
            _docker.ComposeUp(_release.Directory, Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
            _docker.ComposeStop(_release.Directory, Arg.Any<CancellationToken>());
            _docker.ComposeStart(_release.Directory, Arg.Any<CancellationToken>());
        });
        File.ReadAllText(Marker).ShouldBe("2026-09-29T12:00:00.0000000Z");
        _section.Entries.ShouldHaveSingleItem().Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public async Task Run_LeavesAStackAloneWhoseFilesAreCurrent()
    {
        await Converge(Changed);

        await Converge(Changed);

        await _docker.DidNotReceiveWithAnyArgs().ComposeStop(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_StillOwesARestartThatFailed()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Marker)!);
        File.WriteAllText(Marker, "2026-09-28T00:00:00.0000000Z");
        _docker.ComposeStart(Arg.Any<IDirectory>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("start failed")));

        await Should.ThrowAsync<InvalidOperationException>(() => Converge(Changed));

        File.ReadAllText(Marker).ShouldBe("2026-09-28T00:00:00.0000000Z");
    }

    [Fact]
    public async Task Run_RehearsesWithoutRestartingOrRecording()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Marker)!);
        File.WriteAllText(Marker, "2026-09-28T00:00:00.0000000Z");

        await Converge(Changed, dryRun: true);

        await _docker.DidNotReceiveWithAnyArgs().ComposeStop(default!, TestContext.Current.CancellationToken);
        File.ReadAllText(Marker).ShouldBe("2026-09-28T00:00:00.0000000Z");
    }
}
