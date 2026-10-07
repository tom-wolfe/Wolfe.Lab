using Microsoft.Extensions.Options;
using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Workflows.Docker.Models;
using Wolfe.Lab.Application.Workflows.Docker.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Docker.Steps;

public class ConvergeStackTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-root-");
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly ReportSection _section = new("Artifacts");
    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();
    private readonly DeploymentUnit _unit = Catalogs.Unit("monitoring/grafana/compose", Catalogs.Docker("server", "grafana"));

    private static readonly DateTimeOffset Changed = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    public ConvergeStackTests()
    {
        _root.CreateSubdirectory("grafana-server");
        _report.Section(Arg.Any<string>()).Returns(_section);
    }

    public void Dispose() => _root.Delete(recursive: true);

    private string Installed => Path.Combine(_root.FullName, "grafana-server");

    private string Marker => Path.Combine(_root.FullName, ".applied", "grafana-server");

    private Task<StepResult> Converge(DateTimeOffset? stamp, bool dryRun = false) =>
        new ConvergeStack(_docker, Options.Create(new LabDirectories { Root = new PhysicalDirectory(_root.FullName) }),
                _report, new WorkflowJob("docker", "deploy", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(_unit, new ComposeEnvironment(new Dictionary<string, string>()), new Installation(new PhysicalDirectory(Installed), stamp), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Run_TakesAFirstInstallAsCurrentWithoutRestarting()
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
            _docker.ComposeUp(Arg.Is<IDirectory>(directory => directory.AbsolutePath == Installed), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
            _docker.ComposeStop(Arg.Is<IDirectory>(directory => directory.AbsolutePath == Installed), Arg.Any<CancellationToken>());
            _docker.ComposeStart(Arg.Is<IDirectory>(directory => directory.AbsolutePath == Installed), Arg.Any<CancellationToken>());
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
