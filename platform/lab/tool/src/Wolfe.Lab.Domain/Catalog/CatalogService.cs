namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// A service of the <see cref="Catalog"/>, and its components.
/// </summary>
/// <param name="Area">The area it is classified under: its directory's parent.</param>
/// <param name="Declaration">Its catalog entry.</param>
/// <param name="Source">Where it is declared.</param>
/// <param name="Components">Its components, by name.</param>
public sealed record CatalogService(AreaName Area, Service Declaration, DocumentSource Source, IReadOnlyList<CatalogComponent> Components);
