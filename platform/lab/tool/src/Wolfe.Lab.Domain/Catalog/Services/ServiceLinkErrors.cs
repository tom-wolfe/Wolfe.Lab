namespace Wolfe.Lab.Domain.Catalog.Services;

/// <summary>
/// The well-known problems with a link from a service's catalog entry.
/// </summary>
public static class ServiceLinkErrors
{
    /// <summary>
    /// A url link's url is not an absolute http or https URL.
    /// </summary>
    public static Error NotAnHttpUrl(string title, string url) =>
        new($"the link '{title}' has the url '{url}', which is not an absolute http or https URL.");

    /// <summary>
    /// A path link's path is not relative to the file.
    /// </summary>
    public static Error NotARelativePath(string title, string path) => new($"the link '{title}' has the path '{path}', which is not a relative path.");

    /// <summary>
    /// A path link climbs out of the checkout.
    /// </summary>
    public static Error OutOfTheCheckout(string title, string path) => new($"the link '{title}' has the path '{path}', which leads out of the checkout.");

    /// <summary>
    /// A path link leads to nothing in the checkout.
    /// </summary>
    public static Error NotInTheCheckout(string title, string path) => new($"the link '{title}' has the path '{path}', which is not in the checkout.");

    /// <summary>
    /// The link has both a url and a path, or neither.
    /// </summary>
    public static Error UrlOrPath(string title) => new($"the link '{title}' needs a url or a path, not both.");
}
