using System.Text.RegularExpressions;

namespace Wolfe.Lab.Domain.Catalog.Components.Chezmoi;

/// <summary>
/// A machine chezmoi renders the source for: <c>macbook</c>, <c>macmini-node</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct ChezmoiProfile
{
    private static Validation Validate(string input) =>
        Spelling().IsMatch(input) ? Validation.Ok : Validation.Invalid($"'{input}' is no profile: a lower-case letter first, then letters, digits and '-'.");

    [GeneratedRegex("^[a-z][a-z0-9-]*$")]
    private static partial Regex Spelling();
}
