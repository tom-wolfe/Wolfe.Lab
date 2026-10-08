namespace Wolfe.Lab.Domain.Catalog.Components.Garage;

/// <summary>
/// Where a Garage node stands, as its cluster layout names it: copies of a block go to different zones.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct GarageZone
{
    private static Validation Validate(string input) =>
        input.Length > 0 && !input.Any(char.IsWhiteSpace)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is no zone: a word, without spaces.");
}
