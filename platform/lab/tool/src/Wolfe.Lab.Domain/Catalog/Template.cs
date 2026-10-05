using System.Text.RegularExpressions;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Represents a string template that may be expanded when the values are known.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct Template
{
    /// <summary>
    /// The placeholders it holds, by name, in order.
    /// </summary>
    public IReadOnlyList<string> Placeholders => [.. MatchPlaceholders().Matches(Value).Select(match => match.Groups["name"].Value)];

    /// <summary>
    /// Replaces placeholders in the template based on the given values, and returns the result.
    /// </summary>
    public Template Expand(Func<string, string?> value) =>
        From(MatchPlaceholders().Replace(Value, match => value(match.Groups["name"].Value) ?? match.Value));

    /// <summary>
    /// Matches a placholder.
    /// </summary>
    [GeneratedRegex(@"\{(?<name>[a-z][a-z0-9-]*(?:\.[a-z][a-z0-9-]*)*)\}")]
    private static partial Regex MatchPlaceholders();
}
