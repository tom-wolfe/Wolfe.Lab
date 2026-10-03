using Json.Schema.Generation;
using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component, as its file writes it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record ComponentDocument
{
    [Required, Description("What the component is in general.")]
    public ComponentKind Kind { get; init; }

    [Required, Description("What the component is in particular: one of its kind's types.")]
    public string Type { get; init; } = "";

    [Pattern(LabSchema.NamePattern), Description("The component's name. One in a directory of its own is named for it and needs none; one beside its service's declaration must say it.")]
    public string? Name { get; init; }

    [Description("The name people read, when the name is not it.")]
    public string? DisplayName { get; init; }

    [Description("What the component is, in a sentence.")]
    public string? Description { get; init; }

    [Pattern(LabSchema.NamePattern, GenericParameter = 0), UniqueItems(true), Description("The components of this service this one needs, by name.")]
    public List<string>? DependsOn { get; init; }
}
