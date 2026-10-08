using System.Globalization;
using Wolfe.Lab.Domain.Catalog.Components.Models;

namespace Wolfe.Lab.Infrastructure.Ollama;

/// <summary>
/// What a model on the node is made of, as <c>ollama show --modelfile</c> prints it: the weights it
/// is built from, and the parameters it runs with.
/// </summary>
/// <param name="Weights">Each <c>FROM</c>: the blobs it is built from, which two names share when one was made from the other.</param>
/// <param name="Parameters">Each <c>PARAMETER</c>, by name; one given several times holds its last.</param>
public sealed record OllamaModelfile(IReadOnlyList<string> Weights, IReadOnlyDictionary<string, string> Parameters)
{
    /// <summary>
    /// The context it runs with, when it sets one.
    /// </summary>
    public ContextLength? Context =>
        Parameters.TryGetValue("num_ctx", out var value) && int.TryParse(value, CultureInfo.InvariantCulture, out var tokens) && ContextLength.TryFrom(tokens) is { IsSuccess: true } context
            ? context.ValueObject
            : null;

    /// <summary>
    /// Reads <c>ollama show --modelfile</c>'s output. Only lines that start with the keyword count,
    /// outside any triple-quoted block, so the comments and the template's body are passed over.
    /// </summary>
    public static OllamaModelfile Parse(string output)
    {
        var weights = new List<string>();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        var quoted = false;
        foreach (var line in output.Split('\n').Select(line => line.TrimEnd('\r')))
        {
            // A template, a system prompt or a licence spans lines between triple quotes, and
            // nothing inside one is an instruction.
            var wasQuoted = quoted;
            if (CountQuotes(line) % 2 == 1)
            {
                quoted = !quoted;
            }

            if (wasQuoted)
            {
                continue;
            }

            if (line.StartsWith("FROM ", StringComparison.Ordinal))
            {
                weights.Add(line["FROM ".Length..].Trim());
            }
            else if (line.StartsWith("PARAMETER ", StringComparison.Ordinal) && line["PARAMETER ".Length..].Trim().Split(' ', 2) is [var name, var value])
            {
                parameters[name] = value.Trim();
            }
        }

        return new OllamaModelfile(weights, parameters);
    }

    private static int CountQuotes(string line)
    {
        var count = 0;
        for (var at = line.IndexOf("\"\"\"", StringComparison.Ordinal); at >= 0; at = line.IndexOf("\"\"\"", at + 3, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
