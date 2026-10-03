using Vogen;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// A component's name within its service: its directory's when it has one — <c>compose</c>,
/// <c>dnd-vault</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct ComponentName
{
    private static Validation Validate(string input) => NameRule.Validate(input, "a component's name");
}
