using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// Handles reading YAML documents as JSON, with source attribution for errors.
/// </summary>
/// <param name="Documents">The documents, in order.</param>
public sealed partial record YamlDocuments(IReadOnlyList<YamlDocuments.Document> Documents)
{
    /// <summary>
    /// The documents of <paramref name="yaml"/>, or where it stops being YAML.
    /// </summary>
    /// <remarks>
    /// A plain scalar is typed as YAML 1.2's core schema types it — <c>8081</c> a number,
    /// <c>true</c> a boolean, <c>~</c> null — and a quoted one is a string whatever it holds, so
    /// <c>"8081"</c> stays the string it was written as.
    /// </remarks>
    public static Result<YamlDocuments> Parse(string yaml)
    {
        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(yaml));
        }
        catch (YamlException e)
        {
            return new Error($"line {e.Start.Line}: not YAML: {e.InnerException?.Message ?? e.Message}");
        }

        return new YamlDocuments([
            .. stream.Documents.Select(document =>
            {
                var lines = new Dictionary<string, int>(StringComparer.Ordinal);
                return new Document(Convert(document.RootNode, "", lines), lines);
            })
        ]);
    }

    private static JsonNode? Convert(YamlNode node, string pointer, Dictionary<string, int> lines)
    {
        lines[pointer] = (int)node.Start.Line;
        switch (node)
        {
            case YamlMappingNode mapping:
                var json = new JsonObject();
                foreach (var (key, value) in mapping.Children)
                {
                    var name = key is YamlScalarNode { Value: { } text } ? text : key.ToString();
                    json[name] = Convert(value, $"{pointer}/{Escape(name)}", lines);
                }

                return json;
            case YamlSequenceNode sequence:
                return new JsonArray([.. sequence.Children.Select((item, index) => Convert(item, $"{pointer}/{index}", lines))]);
            case YamlScalarNode scalar:
                return Scalar(scalar);
            default:
                return null;
        }
    }

    private static JsonNode? Scalar(YamlScalarNode scalar)
    {
        var text = scalar.Value ?? "";
        if (scalar.Style is not (ScalarStyle.Plain or ScalarStyle.Any))
        {
            return JsonValue.Create(text);
        }

        return text switch
        {
            "" or "~" or "null" or "Null" or "NULL" => null,
            "true" or "True" or "TRUE" => JsonValue.Create(true),
            "false" or "False" or "FALSE" => JsonValue.Create(false),
            _ when Integer().IsMatch(text) && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var whole) => JsonValue.Create(whole),
            _ when Float().IsMatch(text) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => JsonValue.Create(number),
            _ => JsonValue.Create(text)
        };
    }

    // RFC 6901: a pointer's own characters, escaped where a key holds them.
    private static string Escape(string name) => name.Replace("~", "~0").Replace("/", "~1");

    [GeneratedRegex("^[-+]?[0-9]+$")]
    private static partial Regex Integer();

    [GeneratedRegex(@"^[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?$")]
    private static partial Regex Float();

    /// <summary>
    /// One document of the file.
    /// </summary>
    /// <param name="Root">The document as JSON; null for an empty one.</param>
    /// <param name="Lines">The line each value starts on, by its JSON pointer.</param>
    public sealed record Document(JsonNode? Root, IReadOnlyDictionary<string, int> Lines)
    {
        /// <summary>
        /// The line a value was written on, or the nearest enclosing value's.
        /// </summary>
        public int LineOf(string pointer)
        {
            for (var at = pointer; ; at = at[..Math.Max(0, at.LastIndexOf('/'))])
            {
                if (Lines.TryGetValue(at, out var line))
                {
                    return line;
                }

                if (at.Length == 0)
                {
                    return 1;
                }
            }
        }
    }
}
