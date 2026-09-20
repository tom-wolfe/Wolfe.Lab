using Vogen;

namespace Wolfe.Lab.Build.Ollama.Models;

/// <summary>
/// A model as ollama names it — <c>qwen3:8b</c>. Tagged always: an untagged name resolves to
/// whatever <c>latest</c> points at today, which is the opposite of what every other pin in
/// the lab does.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct OllamaModel
{
    /// <summary>
    /// The model a line of output names, or null when the line does not name one — which is
    /// how a table's header and its blank lines are skipped without matching on either.
    /// </summary>
    /// <param name="input">The candidate name.</param>
    public static OllamaModel? TryParse(string? input) =>
        input is { } candidate && IsWellFormed(candidate.Trim()) ? From(candidate.Trim()) : null;

    private static string NormalizeInput(string input) => input.Trim();

    private static bool IsWellFormed(string input)
    {
        var parts = input.Split(':');
        return parts.Length == 2 && parts.All(part => part.Length > 0) && !input.Any(char.IsWhiteSpace);
    }

    private static Validation Validate(string input) =>
        IsWellFormed(input)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' must be <model>:<tag>, so the version the lab runs is the version it declared.");
}
