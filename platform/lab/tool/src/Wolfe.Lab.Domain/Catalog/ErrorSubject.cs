namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// What a <see cref="CatalogError"/> is about: whose check it is.
/// </summary>
public enum ErrorSubject
{
    /// <summary>
    /// Not known: the file did not read far enough to say what it declares.
    /// </summary>
    Unknown,

    /// <summary>
    /// A service's catalog entry, which every check of the service's components judges too.
    /// </summary>
    Service,

    /// <summary>
    /// A component's declaration, which its own check judges.
    /// </summary>
    Component
}
