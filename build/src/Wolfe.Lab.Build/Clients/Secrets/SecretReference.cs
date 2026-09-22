using Vogen;

namespace Wolfe.Lab.Build.Clients.Secrets;

/// <summary>
/// A 1Password reference — <c>op://vault/item/field</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct SecretReference
{
    private const string Scheme = "op://";

    private static string NormalizeInput(string input) => input.Trim();

    private static Validation Validate(string input)
    {
        if (!input.StartsWith(Scheme, StringComparison.Ordinal))
        {
            return Validation.Invalid($"'{input}' is not an {Scheme} reference.");
        }

        var parts = input[Scheme.Length..].Split('/');
        return parts.Length == 3 && !parts.Any(string.IsNullOrWhiteSpace)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' must be {Scheme}<vault>/<item>/<field>.");
    }
}
