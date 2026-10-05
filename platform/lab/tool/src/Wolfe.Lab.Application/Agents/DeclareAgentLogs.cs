using System.Text.Json;
using Wolfe.Lab.Domain;
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
        if (!unit.ByWorkflow(job.WorkflowName).TryGetValue(out var component, out var unowned))
        {
            return StepResult.Failed(unowned);
        }

        var targetsFile = roots.Value.Logs.GetFile(component.QualifiedName + ".json");
        var targets = Targets(plan, component);
        var earlier = await Earlier(targetsFile, plan, component, ct);
        if (job.DryRun)
        {
            log.Skipped(targets.Count == 0
                ? $"Would declare no log files for {component}."
                : $"Would declare {Files(targets.Count)} for {component}: {string.Join(", ", targets.Select(t => t.Labels[LogTargetFile.PathLabel]))}.");
            if (earlier.Count > 0)
            {
                log.Skipped($"Would retire {string.Join(", ", earlier.Select(file => file.Name))}, declared for its agents under an earlier name.");
            }

            return StepResult.Successful;
        }

        // An earlier name's targets would have the collector read the same logs twice, labelled
        // as two components.
        foreach (var file in earlier)
        {
            file.Delete();
            log.Status($"Retired {file.Name}, declared for {component}'s agents under an earlier name.");
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
    /// The other target files that declare only these agents, of this service: what a deploy
    /// wrote before the component, or its directory, was renamed.
    /// </summary>
    private async Task<IReadOnlyList<IFile>> Earlier(IFile own, AgentPlan plan, Component component, CancellationToken ct)
    {
        var agents = plan.Agents.Select(agent => agent.Label.Name).ToHashSet(StringComparer.Ordinal);
        var earlier = new List<IFile>();
        foreach (var file in roots.Value.Logs.GetFiles("*.json").Where(file => file.Name != own.Name))
        {
            if (await file.ReadAllTextIfExists(ct) is not { } text || !LogTargetFile.Read(text).TryGetValue(out var read, out _) || read.Targets.Count == 0)
            {
                continue;
            }

            if (read.Targets.All(target => Labelled(target, TelemetryAttribute.Area) == component.Area.Value
                                           && Labelled(target, TelemetryAttribute.Service) == component.Service.Name.Value
                                           && Labelled(target, TelemetryAttribute.ServiceName) is { } name && agents.Contains(name)))
            {
                earlier.Add(file);
            }
        }

        return earlier;
    }

    private static string? Labelled(LogTarget target, TelemetryAttribute attribute) => target.Labels.GetValueOrDefault(attribute.Label);

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
