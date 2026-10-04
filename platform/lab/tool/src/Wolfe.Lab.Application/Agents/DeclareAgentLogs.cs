using System.Text.Json;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Telemetry;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Application.Agents;

/// <summary>
/// Tells the node's collector which log files the component's agents write, and where in the lab they live.
/// </summary>
[Step("declare agent logs", StepKind.Publish)]
internal sealed class DeclareAgentLogs(IOptions<LabDirectories> roots, WorkflowJob job, IWorkflowLog log)
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public async Task<StepResult> Run(AgentPlan plan, DeploymentUnit unit, CancellationToken ct = default)
    {
        var component = unit.ByWorkflow(job.WorkflowName);
        if (component.IsError)
        {
            return StepResult.Failed(component.Errors);
        }

        var targetsFile = roots.Value.Logs.GetFile(component.Value.QualifiedName + ".json");
        var targets = Targets(plan, component.Value);
        if (job.DryRun)
        {
            log.Skipped(targets.Count == 0
                ? $"Would declare no log files for {component}."
                : $"Would declare {Files(targets.Count)} for {component}: {string.Join(", ", targets.Select(t => t.Labels[LogTargetFile.PathLabel]))}.");
            return StepResult.Successful;
        }

        if (targets.Count == 0)
        {
            targetsFile.Delete();
            log.Detail($"{component} writes no log files.");
            return StepResult.Successful;
        }

        await targetsFile.WriteAllText(JsonSerializer.Serialize(targets, Indented), cancellationToken: ct);
        log.Status($"Declared {Files(targets.Count)} for {component}.");
        return StepResult.Successful;
    }

    /// <summary>
    /// One target per agent that keeps a log: its path, its name and where the component lives,
    /// each attribute spelled as the store's labels are (<see cref="TelemetryAttribute.Label"/>).
    /// </summary>
    private static IReadOnlyList<LogTarget> Targets(AgentPlan plan, Component component) =>
    [
        .. plan.Agents
            .Where(agent => agent.Log is not null)
            .OrderBy(agent => agent.Label.Name, StringComparer.Ordinal)
            .Select(agent => new LogTarget(["localhost"], new Dictionary<string, string>([
                new KeyValuePair<string, string>(LogTargetFile.PathLabel, agent.Log?.Value ?? ""),
                new KeyValuePair<string, string>(TelemetryAttribute.ServiceName.Label, agent.Label.Name),
                .. component.Attributes.Select(attribute => KeyValuePair.Create(attribute.Key.Label, attribute.Value))
            ])))
    ];

    private static string Files(int count) => $"{count} log file{(count == 1 ? "" : "s")}";
}
