using Json.Schema.Generation;
using Wolfe.Lab.Domain.Catalog.Services;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A link from a service's catalog entry.
/// </summary>
[AdditionalProperties(false)]
internal sealed record LinkDocument
{
    [Required, MinLength(1), Description("What the link is called on the page.")]
    public string Title { get; init; } = "";

    [Required, Description("What the link points at.")]
    public LinkType Type { get; init; }

    [Description("An absolute URL. A link has a url or a path.")]
    public string? Url { get; init; }

    [Description("A path relative to this file, for a file in the repository. A link has a url or a path.")]
    public string? Path { get; init; }
}
