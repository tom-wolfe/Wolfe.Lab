namespace Wolfe.Lab.Domain.Packages;

/// <summary>
/// Represents a version a GitHub package is pinned to.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct PackageVersion
{
    private static Validation Validate(string input) =>
        input.Length > 0 && char.IsAsciiLetterOrDigit(input[0]) && input.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '+' or '-')
            ? Validation.Ok
            : Validation.Invalid(PackageErrors.NotAVersion(input).Message);
}
