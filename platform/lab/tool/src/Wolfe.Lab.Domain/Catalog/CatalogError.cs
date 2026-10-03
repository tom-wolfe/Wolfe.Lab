namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Describes an error in a service catalog file.
/// </summary>
/// <param name="Source">The document.</param>
/// <param name="Message">What is wrong, for whoever has to fix it, without where.</param>
/// <param name="Line">The line of the file it is on, when it is known.</param>
/// <param name="Subject">What the document declares, so a check knows whether it is its own.</param>
public sealed record CatalogError(DocumentSource Source, string Message, int? Line = null, ErrorSubject Subject = ErrorSubject.Unknown)
    : Error(Located(Source, Message, Line), null)
{
    /// <summary>
    /// The line of the file it is on, when it is known.
    /// </summary>
    public int? Line { get; init; } = Line;

    /// <summary>
    /// A problem with a service's catalog entry.
    /// </summary>
    public static CatalogError OfService(DocumentSource source, string problem, int? line = null) => new(source, problem, line, ErrorSubject.Service);

    /// <summary>
    /// A problem with a component's declaration.
    /// </summary>
    public static CatalogError OfComponent(DocumentSource source, string problem, int? line = null) => new(source, problem, line, ErrorSubject.Component);

    private static string Located(DocumentSource source, string problem, int? line) =>
        line is { } at ? $"{source}:{at}: {problem}" : $"{source}: {problem}";
}
