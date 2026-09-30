using System.Text.Json;
using Wolfe.Lab.Clients.Releases;
using Wolfe.Lab.Values;

namespace Wolfe.Lab.Clients.Agents.Steps;

/// <summary>
/// Tells the node's collector which log files the component's agents write, and where in the
/// lab they live.
/// </summary>
/// <remarks>
/// A target file per component in <c>${LAB_ROOT}/.logs</c>, in the file discovery format the
/// collector watches (monitoring/alloy): each agent's <c>log</c>, named for the agent and
/// labelled with the component's placement. The whole file is rewritten on every deploy, so an
/// agent the component no longer declares, or no longer gives a log, stops being read; one
/// whose logs go to the journal instead — a Linux agent with no <c>log</c> — needs no file.
/// </remarks>
[Step("declare agent logs", StepKind.Publish)]
internal sealed class DeclareAgentLogs(WorkflowEnvironment environment, WorkflowJob job, IWorkflowLog log)
{
    /// <summary>
    /// The label the collector reads a target's file from — its own name, not the lab's.
    /// </summary>
    internal const string PathLabel = "__path__";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public async Task<StepResult> Run(AgentPlan plan, Component component, CancellationToken ct = default)
    {
        var file = Path.Combine(LabRoots.From(environment).Logs, component.QualifiedName + ".json");
        var targets = Targets(plan, component);
        if (job.DryRun)
        {
            log.Skipped(targets.Count == 0
                ? $"Would declare no log files for {component}."
                : $"Would declare {Files(targets.Count)} for {component}: {string.Join(", ", targets.Select(t => t.Labels[PathLabel]))}.");
            return StepResult.Successful;
        }

        if (targets.Count == 0)
        {
            File.Delete(file);
            log.Detail($"{component} writes no log files.");
            return StepResult.Successful;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(file) ?? LabRoots.From(environment).Logs);
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(targets, Indented), ct);
        log.Status($"Declared {Files(targets.Count)} for {component}.");
        return StepResult.Successful;
    }

    /// <summary>
    /// One target per agent that keeps a log: its path, its name and where the component lives,
    /// each attribute spelled as the store's labels are (<see cref="TelemetryAttribute.Label"/>).
    /// </summary>
    internal static IReadOnlyList<LogTarget> Targets(AgentPlan plan, Component component) =>
    [
        .. plan.Agents
            .Where(agent => agent.Log is not null)
            .OrderBy(agent => agent.Label.Name, StringComparer.Ordinal)
            .Select(agent => new LogTarget(["localhost"], new Dictionary<string, string>([
                new(PathLabel, agent.Log?.Value ?? ""),
                new(TelemetryAttribute.ServiceName.Label, agent.Label.Name),
                .. component.Attributes.Select(attribute => KeyValuePair.Create(attribute.Key.Label, attribute.Value))
            ])))
    ];

    private static string Files(int count) => $"{count} log file{(count == 1 ? "" : "s")}";

    /// <summary>
    /// One entry of a file discovery target file: the collector reads <c>__path__</c> and keeps
    /// the rest as the stream's labels.
    /// </summary>
    internal sealed record LogTarget(
        [property: System.Text.Json.Serialization.JsonPropertyName("targets")] IReadOnlyList<string> Targets,
        [property: System.Text.Json.Serialization.JsonPropertyName("labels")] IReadOnlyDictionary<string, string> Labels);
}
