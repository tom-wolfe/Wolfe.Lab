using Vogen;

namespace Wolfe.Lab.Build.Values;

/// <summary>
/// Where a service answers: an absolute <c>http</c> or <c>https</c> URL.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct ServiceUrl
{
    private static string NormalizeInput(string input) => input.Trim().TrimEnd('/');

    private static Validation Validate(string input) =>
        Uri.TryCreate(input, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not an http or https URL.");
}
