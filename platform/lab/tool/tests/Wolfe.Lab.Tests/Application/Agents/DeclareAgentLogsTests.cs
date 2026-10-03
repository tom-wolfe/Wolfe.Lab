using System.Text.Json;
using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Agents;

public class DeclareAgentLogsTests : IDisposable
{
    private static readonly DeploymentUnit Server = Catalogs.Unit("ai/ollama/server", Catalogs.Definition("server", ComponentKind.Model, WorkflowName.Ollama));

    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-logs-");
    private readonly IOptions<LabDirectories> _roots;

    public DeclareAgentLogsTests() => _roots = Options.Create(new LabDirectories { Root = new PhysicalDirectory(_root.FullName) });

    public void Dispose() => _root.Delete(recursive: true);

    private static AgentDefinition Agent(string name, string? log) => new(
        AgentLabel.ForName(name) ?? throw new ArgumentException(name),
        HostPath.From("/opt/agent"), [], new Dictionary<string, string>(), null,
        log is null ? null : HostPath.From(log), KeepAlive: true, ExitTimeout: null, AgentRestart.Reload, DateTimeOffset.UnixEpoch);

    private Task<StepResult> Declare(bool dryRun = false, params AgentDefinition[] agents) =>
        new DeclareAgentLogs(_roots, new WorkflowJob("ollama", "deploy", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>())
            .Run(new AgentPlan(agents), Server, TestContext.Current.CancellationToken);

    private string TargetFile => Path.Combine(_root.FullName, ".logs", "ai-ollama-server.json");

    [Fact]
    public async Task Run_NamesEachLogFileForItsAgentAndWhereItLives()
    {
        (await Declare(agents: Agent("ollama", "/Users/lab/Library/Logs/ollama.log"))).IsFailure.ShouldBeFalse();

        using var written = JsonDocument.Parse(await File.ReadAllTextAsync(TargetFile, TestContext.Current.CancellationToken));
        var target = written.RootElement.EnumerateArray().ShouldHaveSingleItem();
        target.GetProperty("targets").EnumerateArray().Select(t => t.GetString()).ShouldBe(["localhost"]);
        var labels = target.GetProperty("labels");
        labels.GetProperty("__path__").GetString().ShouldBe("/Users/lab/Library/Logs/ollama.log");
        labels.GetProperty("service_name").GetString().ShouldBe("ollama");
        labels.GetProperty("lab_area").GetString().ShouldBe("ai");
        labels.GetProperty("lab_service").GetString().ShouldBe("ollama");
        labels.GetProperty("lab_component").GetString().ShouldBe("server");
    }

    [Fact]
    public async Task Run_LeavesOutAnAgentWhoseLogsGoToTheJournal()
    {
        await Declare(agents: [Agent("ollama", "/var/log/ollama.log"), Agent("beszel", null)]);

        using var written = JsonDocument.Parse(await File.ReadAllTextAsync(TargetFile, TestContext.Current.CancellationToken));
        written.RootElement.GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task Run_RemovesTheFileWhenNoAgentKeepsALogAnyMore()
    {
        await Declare(agents: Agent("ollama", "/var/log/ollama.log"));

        await Declare(agents: Agent("ollama", null));

        File.Exists(TargetFile).ShouldBeFalse();
    }

    [Fact]
    public async Task Run_WritesNothingWhenRehearsing()
    {
        await Declare(dryRun: true, Agent("ollama", "/var/log/ollama.log"));

        File.Exists(TargetFile).ShouldBeFalse();
    }
}
