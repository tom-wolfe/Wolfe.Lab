namespace Wolfe.Lab.Domain.Catalog.Components.Models;

/// <summary>
/// How much of a conversation a model holds at once, in tokens.
/// </summary>
[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct ContextLength
{
    private static Validation Validate(int input) =>
        input > 0 ? Validation.Ok : Validation.Invalid($"{input} is no context: a length in tokens, above zero.");
}
