using System.Text.Json;
using Wolfe.Lab.Domain.Telemetry;

namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// A file of the collector's discovery targets: what an agent deploy writes to
/// <c>${LAB_ROOT}/.logs</c> for its agents, and chezmoi for the runner.
/// </summary>
/// <param name="Targets">The files it declares, each with its stream's labels.</param>
public sealed record LogTargetFile(IReadOnlyList<DiscoveryTarget> Targets)
{
    /// <summary>
    /// The label the collector reads a target's file from — its own name, not the lab's.
    /// </summary>
    public const string PathLabel = "__path__";

    /// <summary>
    /// The targets in <paramref name="json"/>, when each labels its stream only with attributes
    /// the lab knows; the collector's own labels, <c>__path__</c> and the like, are left to it.
    /// JSON that is not a list of targets — a <c>ritten.json</c>, a template of one — declares none.
    /// </summary>
    public static Result<LogTargetFile> Read(string json)
    {
        IReadOnlyList<DiscoveryTarget>? targets;
        try
        {
            targets = JsonSerializer.Deserialize<List<DiscoveryTarget>>(json);
        }
        catch (JsonException)
        {
            return new LogTargetFile([]);
        }

        var errors = (targets ?? [])
            .SelectMany(target => target.Labels.Keys)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .Where(label => !label.StartsWith("__", StringComparison.Ordinal))
            .SelectMany(label => TelemetryAttribute.Labelled(label).Errors ?? [])
            .Select(error => new Error($"a target's label: {error.Message}"))
            .ToList();
        return errors.Count > 0 ? errors : new LogTargetFile(targets ?? []);
    }
}
