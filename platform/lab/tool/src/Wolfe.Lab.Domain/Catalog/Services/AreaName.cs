namespace Wolfe.Lab.Domain.Catalog.Services;

/// <summary>
/// Provides a grouping mechanism for services.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct AreaName
{
    private static Validation Validate(string input) => NameRule.Validate(input, "an area");
}
