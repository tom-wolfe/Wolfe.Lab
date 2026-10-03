using System.Text.Json.Serialization;

namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// One entry of a discovery target file: the collector reads <see cref="LogTargetFile.PathLabel"/>
/// and keeps the rest as the stream's labels.
/// </summary>
/// <param name="Targets">The hosts it is read on: <c>localhost</c>.</param>
/// <param name="Labels">The stream's labels, and the file's path.</param>
public sealed record LogTarget(
    [property: JsonPropertyName("targets")] IReadOnlyList<string> Targets,
    [property: JsonPropertyName("labels")] IReadOnlyDictionary<string, string> Labels
);
