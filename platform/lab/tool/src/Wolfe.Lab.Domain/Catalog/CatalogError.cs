namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Describes an error in a service catalog file: a well-known <see cref="Problem"/>, and where it is.
/// </summary>
/// <remarks>
/// The problem is held as it is, so a caller can ask which it is
/// (<c>error.Problem == ComponentErrors.DependsOnItself(name)</c>); only the message says where.
/// </remarks>
/// <param name="Source">The document.</param>
/// <param name="Problem">What is wrong, for whoever has to fix it, without where.</param>
/// <param name="Line">The line of the file it is on, when it is known.</param>
/// <param name="Field">The field of the document it is in, when it is one field's.</param>
public sealed record CatalogError(DocumentSource Source, Error Problem, int? Line = null, string? Field = null)
    : Error(Located(Source, Problem, Line, Field), null)
{
    /// <summary>
    /// The line of the file it is on, when it is known.
    /// </summary>
    public int? Line { get; init; } = Line;

    /// <summary>
    /// <paramref name="error"/> in <paramref name="source"/>: a field's, when it is a <see cref="FieldError"/>.
    /// </summary>
    public static CatalogError In(DocumentSource source, Error error, int? line = null) =>
        error is FieldError field
            ? new CatalogError(source, field.Problem, line, field.Field)
            : new CatalogError(source, error, line);

    private static string Located(DocumentSource source, Error problem, int? line, string? field)
    {
        var at = line is { } number ? $"{source}:{number}" : source.ToString();
        return field is null ? $"{at}: {problem.Message}" : $"{at}: {field}: {problem.Message}";
    }
}
