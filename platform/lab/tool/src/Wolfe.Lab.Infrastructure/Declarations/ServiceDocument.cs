using Json.Schema.Generation;
using Wolfe.Lab.Domain.Catalog.Services;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A service's catalog entry, as its file writes it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record ServiceDocument
{
    [Required, Const("service"), Description("A service: the unit the catalog lists, declared in its own directory.")]
    public string Kind { get; init; } = "";

    [Required, Pattern(LabSchema.NamePattern), Description("The service's name, which is its directory's.")]
    public string Name { get; init; } = "";

    [Description("The name people read, when the name is not it.")]
    public string? DisplayName { get; init; }

    [Required, MinLength(1), Description("What the service is, in a sentence.")]
    public string Description { get; init; } = "";

    [Description("Where the service is in its life; production unless it says otherwise.")]
    public Lifecycle? Lifecycle { get; init; }

    [Description("Where to use the service, read about it and watch it.")]
    public List<LinkDocument>? Links { get; init; }

    [Pattern(LabSchema.NamePattern, GenericParameter = 0), UniqueItems(true), Description("The services this one needs, by name.")]
    public List<string>? DependsOn { get; init; }
}
