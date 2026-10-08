namespace Wolfe.Lab.Domain.Catalog.Components.Models;

/// <summary>
/// A model as its server names it — <c>qwen3.6:35b-a3b</c>. Tagged always: an untagged name
/// resolves to whatever <c>latest</c> points at today, which is the opposite of a pin.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct ModelTag
{
    private static string NormalizeInput(string input) => input.Trim();

    private static Validation Validate(string input) =>
        input.Split(':') is [{ Length: > 0 }, { Length: > 0 }] && !input.Any(char.IsWhiteSpace)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' must be <model>:<tag>, so the version the lab runs is the version it declared.");
}
