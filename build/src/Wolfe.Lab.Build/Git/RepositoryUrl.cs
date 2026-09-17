using Vogen;

namespace Wolfe.Lab.Build.Git;

/// <summary>
/// Where a repository is: an absolute <c>http</c>, <c>https</c> or <c>ssh</c> URL, since those
/// are what git takes on the command line and what a credential helper can answer for.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct RepositoryUrl
{
    private static string NormalizeInput(string input) => input.Trim();

    private static Validation Validate(string input) =>
        Uri.TryCreate(input, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "ssh"
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not an http, https or ssh repository URL.");
}
