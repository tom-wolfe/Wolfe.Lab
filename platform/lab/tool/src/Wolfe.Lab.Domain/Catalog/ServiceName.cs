using Vogen;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// A service's name: its directory's, and its catalog entry's — <c>immich</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct ServiceName
{
    private static Validation Validate(string input) => NameRule.Validate(input, "a service's name");
}
