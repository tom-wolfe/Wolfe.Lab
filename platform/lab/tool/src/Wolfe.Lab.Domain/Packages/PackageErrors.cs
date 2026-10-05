namespace Wolfe.Lab.Domain.Packages;

/// <summary>
/// The well-known problems with naming a package.
/// </summary>
public static class PackageErrors
{
    /// <summary>
    /// The value is not a GitHub repository.
    /// </summary>
    public static Error NotARepository(string given) => new($"'{given}' is not a GitHub repository: owner/name.");

    /// <summary>
    /// The value is not a version.
    /// </summary>
    public static Error NotAVersion(string given) =>
        new($"'{given}' is not a version: a letter or digit first, then letters, digits, '.', '_', '+' and '-'.");
}
