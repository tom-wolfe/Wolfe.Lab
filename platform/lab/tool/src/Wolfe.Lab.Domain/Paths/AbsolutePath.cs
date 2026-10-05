namespace Wolfe.Lab.Domain.Paths;

/// <summary>
/// Represents an absolute path on a node.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct AbsolutePath
{
    private static Validation Validate(string input) =>
        input.StartsWith('/') && !input.Contains('$')
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not an absolute path: it starts with '/', with no ~ or variables.");
}
