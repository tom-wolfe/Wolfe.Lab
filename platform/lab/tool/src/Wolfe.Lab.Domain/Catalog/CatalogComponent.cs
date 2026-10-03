namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// A component of a <see cref="CatalogService"/>.
/// </summary>
/// <param name="Name">Its name, declared or its directory's.</param>
/// <param name="Declaration">What it is.</param>
/// <param name="Source">Where it is declared.</param>
/// <param name="Directory">Its own directory, <c>area/service/component</c>, or null when it has none.</param>
public sealed record CatalogComponent(ComponentName Name, Component Declaration, DocumentSource Source, RepositoryPath? Directory);
