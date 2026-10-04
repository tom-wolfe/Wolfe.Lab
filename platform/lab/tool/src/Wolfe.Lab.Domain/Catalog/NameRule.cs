namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// The rule every name in the lab's catalog is held to: kebab-case.
/// </summary>
internal static class NameRule
{
    /// <summary>
    /// Whether <paramref name="input"/> is a name, and if not, what kind of name it failed to be.
    /// </summary>
    /// <param name="input">The name.</param>
    /// <param name="what">What it was meant to name, for the message: <c>an area</c>.</param>
    public static Validation Validate(string input, string what) =>
        input.Length > 0 && input[0] is >= 'a' and <= 'z' && input.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not {what}: lower case, a letter first, then letters, digits and hyphens.");
}
