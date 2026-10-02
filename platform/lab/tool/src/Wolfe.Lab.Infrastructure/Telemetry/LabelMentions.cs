using System.Text.RegularExpressions;
using Wolfe.Lab.Domain.Telemetry;

namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// The lab's labels a file names in a store's spelling — in a query, a relabel, an alert rule —
/// where no structure says they are labels, so they are found by their prefix.
/// </summary>
/// <param name="Attributes">The attributes named, each once.</param>
/// <param name="ContainerLabels">The collector's own labels named, each once.</param>
public sealed partial record LabelMentions(IReadOnlyList<TelemetryAttribute> Attributes, IReadOnlyList<ContainerLabel> ContainerLabels)
{
    /// <summary>
    /// Every <c>lab_…</c> label in <paramref name="text"/>, when each is one the lab knows.
    /// </summary>
    /// <param name="text">The file.</param>
    /// <param name="judged">Names a reader that knows the file's structure has already held to
    /// the list, so a wrong one is reported once, where it is understood.</param>
    public static Result<LabelMentions> In(string text, IReadOnlySet<string>? judged = null)
    {
        var attributes = new List<TelemetryAttribute>();
        var labels = new List<ContainerLabel>();
        var errors = new List<Error>();
        foreach (var name in LabLabel().Matches(text).Select(match => match.Value).Distinct().Order(StringComparer.Ordinal))
        {
            if (judged?.Contains(name) == true)
            {
                continue;
            }

            if (TelemetryAttribute.Labelled(name).Value is { } attribute)
            {
                attributes.Add(attribute);
            }
            else if (ContainerLabel.Labelled(name).Value is { } label)
            {
                labels.Add(label);
            }
            else
            {
                errors.Add(new Error($"'{name}' is not a label the lab knows ({Known()})."));
            }
        }

        return errors.Count > 0 ? errors : new LabelMentions(attributes, labels);
    }

    /// <summary>
    /// Every label in a store's spelling the lab has: its attributes', and the collector's own.
    /// </summary>
    internal static string Known() =>
        string.Join(", ", TelemetryAttribute.All.Select(attribute => attribute.Label).Concat(ContainerLabel.All.Select(label => label.Label)));

    [GeneratedRegex("""(?<![A-Za-z0-9_])lab_[a-z0-9_]+""")]
    private static partial Regex LabLabel();
}
