using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Logrotate;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Agents;

public class RotateAgentLogsTests : IDisposable
{
    private static readonly DeploymentUnit Unit = Catalogs.Unit("ai/ollama/mini", Catalogs.Definition("mini", ComponentKind.Model, WorkflowName.Ollama));

    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-rotate-");
    private readonly DirectoryInfo _scratch = Directory.CreateTempSubdirectory("lab-rotate-scratch-");
    private readonly ILogrotate _logrotate = Substitute.For<ILogrotate>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public RotateAgentLogsTests() => _fileSystem.Temp.Returns(new PhysicalDirectory(_scratch.FullName));

    public void Dispose()
    {
        _root.Delete(recursive: true);
        _scratch.Delete(recursive: true);
    }

    private Task<StepResult> Rotate(bool dryRun = false, params (string Name, string? Log)[] agents) =>
        new RotateAgentLogs(_logrotate, Resolvers.On(root: _root.FullName), _fileSystem, Options.Create(new LabDirectories { Root = new PhysicalDirectory(_root.FullName) }),
                new WorkflowJob("ollama", "rotate", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(new AgentDeclarations(agents.ToDictionary(agent => agent.Name, agent => new AgentOptions
            {
                Program = HostPath.From("/opt/agent"),
                Log = agent.Log is { } path ? HostPath.From(path) : null
            })), Unit, TestContext.Current.CancellationToken);

    private string Rotations => Path.Combine(_root.FullName, RotateAgentLogs.Directory);

    [Fact]
    public async Task Run_RotatesTheComponentsLogs_WithItsOwnConfigurationAndState()
    {
        (await Rotate(agents: ("ollama", $"{_root.FullName}/logs/ai-ollama-mini.log"))).IsFailure.ShouldBeFalse();

        var configuration = Path.Combine(Rotations, "ai-ollama-mini.conf");
        (await File.ReadAllTextAsync(configuration, TestContext.Current.CancellationToken)).ShouldStartWith($"\"{_root.FullName}/logs/ai-ollama-mini.log\" {{");
        await _logrotate.Received(1).Rotate(
            Arg.Is<IFile>(file => file.AbsolutePath == configuration),
            Arg.Is<IFile>(file => file.AbsolutePath == Path.Combine(Rotations, "ai-ollama-mini.state")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_RehearsesFromTheRunsScratch_LeavingTheRootAlone()
    {
        await Rotate(dryRun: true, ("ollama", "/var/log/ollama.log"));

        Directory.Exists(Rotations).ShouldBeFalse();
        File.Exists(Path.Combine(_scratch.FullName, RotateAgentLogs.Directory, "ai-ollama-mini.conf")).ShouldBeTrue();
        await _logrotate.Received(1).Rotate(Arg.Any<IFile>(), Arg.Any<IFile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_RotatesNothing_ForAgentsThatWriteNoLog()
    {
        (await Rotate(agents: ("beszel", null))).IsFailure.ShouldBeFalse();

        await _logrotate.DidNotReceive().Rotate(Arg.Any<IFile>(), Arg.Any<IFile>(), Arg.Any<CancellationToken>());
    }
}
