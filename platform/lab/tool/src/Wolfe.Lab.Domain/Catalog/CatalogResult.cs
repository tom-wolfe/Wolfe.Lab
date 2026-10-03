namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Describes the result of reading the service catalog.
/// </summary>
/// <remarks>
/// What holds is kept either way (<see cref="Partial"/>): a check of one component resolves what
/// it names against everything else that holds, and is held to its own problems
/// (<see cref="OfComponentIn"/>), not the whole repository's.
/// </remarks>
public sealed class CatalogResult : Result<Catalog>
{
    private CatalogResult(Catalog partial)
        : base(partial)
    {
        Partial = partial;
        Errors = [];
    }

    private CatalogResult(Catalog partial, IReadOnlyList<CatalogError> errors)
        : base(errors)
    {
        Partial = partial;
        Errors = errors;
    }

    /// <summary>
    /// A reading of <paramref name="holding"/> and <paramref name="errors"/>: a success when there
    /// are none.
    /// </summary>
    public static CatalogResult Of(Catalog holding, IReadOnlyList<CatalogError> errors) =>
        errors.Count > 0 ? new CatalogResult(holding, errors) : new CatalogResult(holding);

    /// <summary>
    /// Every service and component that holds, whatever else does not.
    /// </summary>
    public Catalog Partial { get; }

    /// <summary>
    /// Every problem, typed: what <see cref="Result{T}.Errors"/> holds, as the problems they are.
    /// </summary>
    public new IReadOnlyList<CatalogError> Errors { get; }

    /// <summary>
    /// The catalog as a component's check is held to it: a success unless its own declaration, in
    /// its directory, or its service's entry — the context every one of the service's components
    /// is declared in — has a problem; another component's, though it is declared beside the
    /// entry, is that component's to report.
    /// </summary>
    /// <param name="directory">The component's directory, <c>area/service/component</c>.</param>
    public Result<Catalog> OfComponentIn(RepositoryPath directory)
    {
        var own = Errors
            .Where(problem => directory.Contains(problem.Source.File)
                || (problem.Source.File.Parent == directory.Parent && problem.Subject != ErrorSubject.Component))
            .ToList<Error>();
        return own.Count > 0 ? own : Partial;
    }

    /// <summary>
    /// This reading with more problems, found before the documents could be read together.
    /// </summary>
    public CatalogResult With(IEnumerable<CatalogError> earlier) => Of(Partial, [.. earlier, .. Errors]);
}
