using Vogen;

namespace Wolfe.Lab.Build.Values;

/// <summary>
/// The account a push presents. One word: git hands it to the credential helper as a line of
/// <c>key=value</c>, so a space or a colon would change what the helper reads.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct GitUsername
{
    private static string NormalizeInput(string input) => input.Trim();

    private static Validation Validate(string input) =>
        input.Length > 0 && !input.Any(c => char.IsWhiteSpace(c) || c == ':')
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not a username: one word, no colon.");
}
