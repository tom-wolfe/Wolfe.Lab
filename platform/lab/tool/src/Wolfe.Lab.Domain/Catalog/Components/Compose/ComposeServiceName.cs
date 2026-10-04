namespace Wolfe.Lab.Domain.Catalog.Components.Compose;

/// <summary>
/// A service of a compose stack, as its compose file names it: <c>immich-server</c>.
/// </summary>
/// <remarks>
/// Compose's own rule, not the catalog's: the name is the file's, and a component names it as it is.
/// </remarks>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct ComposeServiceName
{
    private static Validation Validate(string input) =>
        input.Length > 0 && char.IsAsciiLetterOrDigit(input[0]) && input.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '-')
            ? Validation.Ok
            : Validation.Invalid(ComponentErrors.NotAComposeService(input).Message);
}
