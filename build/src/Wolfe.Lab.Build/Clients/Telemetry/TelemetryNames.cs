using System.Text.RegularExpressions;
using Wolfe.Lab.Build.Values;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Wolfe.Lab.Build.Clients.Telemetry;

/// <summary>
/// Holds the configuration files that name the lab's telemetry attributes to the one list of them
/// (<see cref="TelemetryAttribute"/>, <see cref="ContainerLabel"/>).
/// </summary>
/// <remarks>
/// A configuration file cannot read a C# constant, so it spells the names itself; these checks
/// are what keep that spelling from drifting. Each returns what is wrong, one sentence a problem.
/// </remarks>
internal static partial class TelemetryNames
{
    /// <summary>
    /// The one attribute Loki indexes on OTLP logs without being told to.
    /// </summary>
    internal static readonly TelemetryAttribute IndexedByLoki = TelemetryAttribute.ServiceName;

    private static readonly IDeserializer Reader = new DeserializerBuilder().Build();

    /// <summary>
    /// A compose file sets only the labels meant for it, with values the collector understands,
    /// and leaves the deploy's own — where the component lives, and the resource attributes that
    /// say so — to the deploy: set here too, one would silently override the other.
    /// </summary>
    public static IEnumerable<string> Compose(string yaml)
    {
        if (Parse(yaml) is not { } document || At(document, "services") is not Dictionary<object, object> services)
        {
            yield break;
        }

        foreach (var (name, definition) in services)
        {
            foreach (var (label, value) in Pairs(At(definition, "labels")))
            {
                if (TelemetryAttribute.IsName(label))
                {
                    yield return $"service '{name}' sets the label {label}, which the deploy sets from where the component lives.";
                }
                else if (ContainerLabel.All.FirstOrDefault(known => known.Name == label) is { } known)
                {
                    if (!known.Values.Contains(value))
                    {
                        yield return $"service '{name}' sets {label} to '{value}': the collector understands {string.Join(", ", known.Values)}.";
                    }
                }
                else if (label.StartsWith("lab.", StringComparison.Ordinal))
                {
                    yield return $"service '{name}' sets the label {label}, which is not one the lab knows ({Known()}).";
                }
            }

            if (Pairs(At(definition, "environment")).Any(variable => variable.Key == TelemetryAttribute.ResourceAttributesVariable))
            {
                yield return $"service '{name}' sets {TelemetryAttribute.ResourceAttributesVariable}, which the deploy sets from where the component lives.";
            }
        }
    }

    /// <summary>
    /// A collector configuration names attributes only from the list: as OTTL's
    /// <c>attributes["…"]</c>, as a relabel's <c>target_label</c>, and as a Docker label it reads.
    /// </summary>
    public static IEnumerable<string> Alloy(string text)
    {
        foreach (var name in OttlAttribute().Matches(text).Select(match => match.Groups["name"].Value).Distinct())
        {
            if (!TelemetryAttribute.IsName(name))
            {
                yield return $"attributes[\"{name}\"] is not an attribute the lab knows ({Names()}).";
            }
        }

        foreach (var label in TargetLabel().Matches(text).Select(match => match.Groups["label"].Value).Distinct())
        {
            if (!label.StartsWith("__", StringComparison.Ordinal) && !TelemetryAttribute.IsLabel(label))
            {
                yield return $"target_label \"{label}\" is not an attribute the lab knows ({Labels()}).";
            }
        }

        foreach (var label in DockerLabel().Matches(text).Select(match => match.Groups["label"].Value).Distinct())
        {
            if (!TelemetryAttribute.IsLabel(label) && ContainerLabel.All.All(known => known.Label != label))
            {
                yield return $"the Docker label {label} is not one the lab knows ({Known()}).";
            }
        }

        foreach (var problem in LabLabels(text))
        {
            yield return problem;
        }
    }

    /// <summary>
    /// The stores and their front end: Prometheus promotes every attribute, Loki indexes every
    /// attribute, and Grafana's link from a span to its logs pairs an attribute with its own
    /// label. Any other YAML is only held to the label names it uses.
    /// </summary>
    public static IEnumerable<string> Yaml(string text)
    {
        foreach (var problem in LabLabels(text))
        {
            yield return problem;
        }

        if (Parse(text) is not { } document)
        {
            yield break;
        }

        if (At(document, "otlp", "promote_resource_attributes") is List<object> promoted)
        {
            foreach (var problem in Covers("Prometheus's otlp.promote_resource_attributes", Strings(promoted), TelemetryAttribute.All))
            {
                yield return problem;
            }
        }

        if (At(document, "limits_config") is not null)
        {
            var indexed = (At(document, "limits_config", "otlp_config", "resource_attributes", "attributes_config") as List<object> ?? [])
                .Where(entry => At(entry, "action") as string == "index_label")
                .SelectMany(entry => Strings(At(entry, "attributes") as List<object> ?? []));
            var expected = TelemetryAttribute.All.Where(attribute => attribute != IndexedByLoki).ToList();
            foreach (var problem in Covers("Loki's otlp_config index labels", indexed, expected))
            {
                yield return problem;
            }
        }

        foreach (var source in At(document, "datasources") as List<object> ?? [])
        {
            foreach (var tag in At(source, "jsonData", "tracesToLogsV2", "tags") as List<object> ?? [])
            {
                if (At(tag, "key") is string key && At(tag, "value") is string value
                    && TelemetryAttribute.All.FirstOrDefault(attribute => attribute.Name == key) is var attribute
                    && (attribute is null || attribute.Label != value))
                {
                    yield return attribute is null
                        ? $"datasource '{At(source, "name")}' links spans to logs by {key}, which is not an attribute the lab knows ({Names()})."
                        : $"datasource '{At(source, "name")}' links {key} to the label '{value}': Loki spells it {attribute.Label}.";
                }
            }
        }
    }

    /// <summary>
    /// Every <c>lab_…</c> label a file names — in a query, a relabel — is one the lab knows.
    /// </summary>
    private static IEnumerable<string> LabLabels(string text)
    {
        foreach (var label in LabLabel().Matches(text).Select(match => match.Value).Distinct())
        {
            if (!TelemetryAttribute.IsLabel(label) && ContainerLabel.All.All(known => known.Label != label))
            {
                yield return $"{label} is not a label the lab knows ({Labels()}).";
            }
        }
    }

    /// <summary>
    /// A list that must name every one of <paramref name="expected"/> and nothing else, so a new
    /// attribute fails here until the store is told about it.
    /// </summary>
    private static IEnumerable<string> Covers(string what, IEnumerable<string> listed, IReadOnlyList<TelemetryAttribute> expected)
    {
        var names = listed.ToHashSet(StringComparer.Ordinal);
        foreach (var missing in expected.Where(attribute => !names.Contains(attribute.Name)))
        {
            yield return $"{what} leaves out {missing.Name}.";
        }

        foreach (var unknown in names.Where(name => !TelemetryAttribute.IsName(name)).Order(StringComparer.Ordinal))
        {
            yield return $"{what} names {unknown}, which is not an attribute the lab knows ({Names()}).";
        }
    }

    private static object? Parse(string yaml)
    {
        try
        {
            return Reader.Deserialize<object>(yaml);
        }
        catch (YamlException)
        {
            // Whether the file is valid YAML is its own tool's to say; only what it names is judged here.
            return null;
        }
    }

    private static object? At(object? node, params string[] path)
    {
        foreach (var key in path)
        {
            node = node is Dictionary<object, object> map && map.TryGetValue(key, out var next) ? next : null;
        }

        return node;
    }

    private static IEnumerable<string> Strings(IEnumerable<object> items) => items.OfType<string>();

    /// <summary>
    /// A compose mapping written either way compose allows: <c>key: value</c>, or a list of
    /// <c>key=value</c>.
    /// </summary>
    private static IEnumerable<KeyValuePair<string, string>> Pairs(object? node) => node switch
    {
        Dictionary<object, object> map => map.Select(pair => KeyValuePair.Create($"{pair.Key}", $"{pair.Value}")),
        List<object> list => list.OfType<string>().Select(item => item.Split('=', 2) is [var key, var value]
            ? KeyValuePair.Create(key, value)
            : KeyValuePair.Create(item, "")),
        _ => []
    };

    private static string Names() => string.Join(", ", TelemetryAttribute.All.Select(attribute => attribute.Name));

    private static string Labels() => string.Join(", ", TelemetryAttribute.All.Select(attribute => attribute.Label).Concat(ContainerLabel.All.Select(label => label.Label)));

    private static string Known() => string.Join(", ", ContainerLabel.All.Select(label => label.Name));

    [GeneratedRegex("""attributes\[\\?"(?<name>[^"\\]+)\\?"\]""")]
    private static partial Regex OttlAttribute();

    [GeneratedRegex(""""target_label\s*=\s*"(?<label>[^"]+)"""")]
    private static partial Regex TargetLabel();

    [GeneratedRegex("""__meta_docker_container_label_(?<label>[a-z0-9_]+)""")]
    private static partial Regex DockerLabel();

    [GeneratedRegex("""(?<![A-Za-z0-9_])lab_[a-z0-9_]+""")]
    private static partial Regex LabLabel();
}
