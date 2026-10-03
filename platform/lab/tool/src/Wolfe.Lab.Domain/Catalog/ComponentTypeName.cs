using Vogen;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// A type of component within its kind, as a declaration writes it: <c>compose</c>, <c>snapshot</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct ComponentTypeName
{
    private static Validation Validate(string input) => NameRule.Validate(input, "a type of component");
}
