namespace Wolfe.Lab.Domain.Catalog.Services;

/// <summary>
/// The well-known problems with a service's catalog entry.
/// </summary>
public static class ServiceErrors
{
    /// <summary>
    /// The entry is not in a directory of the service's own, <c>area/service/</c>.
    /// </summary>
    public static Error NotInItsOwnDirectory { get; } = new("a service is declared in its own directory, area/service/.");

    /// <summary>
    /// The directory above the service's is not one an area can be named for.
    /// </summary>
    public static Error AreaNotAName(string area) =>
        new($"its area's directory, '{area}', is not a name an area can have: lower case, a letter first, then letters, digits and hyphens.");

    /// <summary>
    /// The service's name is not its directory's.
    /// </summary>
    public static Error NamedOtherThanItsDirectory(ServiceName name, string directory) =>
        new($"the service is named '{name}', but its directory is '{directory}'.");

    /// <summary>
    /// Another entry declares the service in the same directory.
    /// </summary>
    public static Error DeclaredAlready(string directory, DocumentSource other) => new($"'{directory}' is declared already, in {other}.");

    /// <summary>
    /// A service in another area has the same name; a service's name is the lab's.
    /// </summary>
    public static Error NameShared(ServiceName name, DocumentSource other) =>
        new($"another service is named '{name}' too ({other}); a service's name is the lab's, not its area's.");

    /// <summary>
    /// It depends on a service the lab does not declare, or one that depends on it in turn.
    /// </summary>
    public static Error DependsOnUndeclared(ServiceName needed) =>
        new($"depends on the service '{needed}', which the lab does not declare, or which depends on this one in turn.");

    /// <summary>
    /// It depends on itself.
    /// </summary>
    public static Error DependsOnItself(ServiceName name) => new($"'{name}' depends on itself.");
}
