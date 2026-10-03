namespace Wolfe.Lab.Domain.Catalog.Services;

/// <summary>
/// Describes a link in a service's catalog entry.
/// </summary>
/// <param name="Title">What it is called on the page.</param>
/// <param name="Type">What it points at.</param>
/// <param name="Target">Where it points: an absolute URL, or a relative one.</param>
public sealed record ServiceLink(string Title, LinkType Type, Uri Target);
