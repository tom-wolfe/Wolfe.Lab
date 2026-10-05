namespace Wolfe.Lab.Domain.Packages;

/// <summary>
/// A repository on GitHub, as its owner and name: <c>grafana/alloy</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct GitHubRepository
{
    /// <summary>
    /// Its owner: <c>grafana</c>.
    /// </summary>
    public string Owner => Value[..Value.IndexOf('/', StringComparison.Ordinal)];

    /// <summary>
    /// Its name within its owner: <c>alloy</c>.
    /// </summary>
    public string Name => Value[(Value.IndexOf('/', StringComparison.Ordinal) + 1)..];

    private static Validation Validate(string input) =>
        input.Split('/') is [{ Length: > 0 } owner, { Length: > 0 } name] && Part(owner) && Part(name)
            ? Validation.Ok
            : Validation.Invalid(PackageErrors.NotARepository(input).Message);

    // GitHub's own rule for an owner's or a repository's name, near enough: never a path's . or ...
    private static bool Part(string part) => part is not ("." or "..") && part.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
