using System.Text.RegularExpressions;
using Wolfe.Lab.Domain.Telemetry;

namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// The lab's telemetry an Alloy configuration names: attributes as OTTL spells them
/// (<c>attributes["lab.role"]</c>), labels a relabel writes (<c>target_label</c>), and the Docker
/// labels it reads (<c>__meta_docker_container_label_…</c>).
/// </summary>
/// <remarks>
/// Alloy's syntax has no parser this side of Alloy, so each kind of name is found by the pattern
/// that marks it; what is held is the names, typed, not the text.
/// </remarks>
/// <param name="Attributes">The attributes named, in either spelling, each once.</param>
/// <param name="ContainerLabels">The collector's own labels it reads, each once.</param>
public sealed partial record AlloyConfig(IReadOnlyList<TelemetryAttribute> Attributes, IReadOnlyList<ContainerLabel> ContainerLabels)
{
    /// <summary>
    /// The names in <paramref name="text"/>, when every one is one the lab knows.
    /// </summary>
    public static Result<AlloyConfig> Read(string text)
    {
        var attributes = new List<TelemetryAttribute>();
        var labels = new List<ContainerLabel>();
        var errors = new List<Error>();
        var judged = new HashSet<string>(StringComparer.Ordinal);

        void Take(Result<TelemetryAttribute> attribute, string where)
        {
            if (attribute.Value is { } found)
            {
                attributes.Add(found);
            }
            else
            {
                errors.AddRange((attribute.Errors ?? []).Select(error => new Error($"{where}: {error.Message}")));
            }
        }

        foreach (var name in Names(OttlAttribute(), text))
        {
            Take(TelemetryAttribute.Named(name), $"attributes[\"{name}\"]");
        }

        // A label of the collector's own (__path__, __address__) is Alloy's business, not the lab's.
        foreach (var label in Names(TargetLabel(), text).Where(label => !label.StartsWith("__", StringComparison.Ordinal)))
        {
            judged.Add(label);
            Take(TelemetryAttribute.Labelled(label), $"target_label \"{label}\"");
        }

        foreach (var label in Names(DockerLabel(), text))
        {
            judged.Add(label);
            if (ContainerLabel.Labelled(label).Value is { } container)
            {
                labels.Add(container);
            }
            else
            {
                Take(TelemetryAttribute.Labelled(label), $"the Docker label {label}");
            }
        }

        if (LabelMentions.In(text, judged) is { Errors: { } unknown })
        {
            errors.AddRange(unknown);
        }

        return errors.Count > 0 ? errors : new AlloyConfig([.. attributes.Distinct()], [.. labels.Distinct()]);
    }

    private static IEnumerable<string> Names(Regex pattern, string text) =>
        pattern.Matches(text).Select(match => match.Groups["name"].Value).Distinct().Order(StringComparer.Ordinal);

    [GeneratedRegex("""attributes\[\\?"(?<name>[^"\\]+)\\?"\]""")]
    private static partial Regex OttlAttribute();

    [GeneratedRegex(""""target_label\s*=\s*"(?<name>[^"]+)"""")]
    private static partial Regex TargetLabel();

    [GeneratedRegex("""__meta_docker_container_label_(?<name>[a-z0-9_]+)""")]
    private static partial Regex DockerLabel();
}
