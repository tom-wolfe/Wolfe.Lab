using Vogen;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Where a service is in its life.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
[Instance("Production", "production", "Relied on.")]
[Instance("Experimental", "experimental", "Being tried; nothing depends on it yet.")]
[Instance("Deprecated", "deprecated", "On its way out.")]
public readonly partial struct Lifecycle : IClosedSet<Lifecycle>
{
    /// <inheritdoc />
    public static IReadOnlyList<Lifecycle> All => [Production, Experimental, Deprecated];

    private static Validation Validate(string input) =>
        All.Any(lifecycle => lifecycle.Value == input)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not a lifecycle ({string.Join(", ", All)}).");
}
