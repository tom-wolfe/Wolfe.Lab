using System.Text.Json.Serialization;

namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// One entry of a discovery target file, as a node's collector reads them: a log file
/// (<see cref="LogTargetFile.PathLabel"/>), or a metrics endpoint
/// (<see cref="MetricsTargetFile.PathLabel"/>), and the labels it carries.
/// </summary>
/// <param name="Targets">Where it is read: <c>localhost</c> for a file, the address scraped for metrics.</param>
/// <param name="Labels">Its labels, and the path the collector reads.</param>
public sealed record DiscoveryTarget(
    [property: JsonPropertyName("targets")] IReadOnlyList<string> Targets,
    [property: JsonPropertyName("labels")] IReadOnlyDictionary<string, string> Labels
);
