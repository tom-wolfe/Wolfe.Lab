namespace Wolfe.Lab.Domain.Catalog.Components.Images;

/// <summary>
/// Where an image is pushed: a repository on a registry it names, so a push never goes to Docker
/// Hub by default (<c>code.twolfe.dev/tom-wolfe/ci</c>).
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct ImageTag
{
    /// <summary>
    /// The registry it is pushed to.
    /// </summary>
    public string Registry => Value.Split('/')[0];

    private static Validation Validate(string input) =>
        input.Split('/') is [var host, _, ..] && (host.Contains('.') || host.Contains(':')) && !input.Any(char.IsWhiteSpace)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' names no registry host, so a push would go to Docker Hub.");
}
