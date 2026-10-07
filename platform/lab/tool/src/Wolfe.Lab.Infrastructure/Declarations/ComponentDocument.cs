using Json.Schema.Generation;
using Wolfe.Lab.Domain.Catalog.Components;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// Represents a component's metadata in a YAML document.
/// </summary>
[AdditionalProperties(false)]
internal record ComponentDocument
{
    [Required, Description("What the component is used for: the icon it would have on a diagram.")]
    public ComponentKind Kind { get; init; }

    [Required, Description("The workflow that operates the component, whatever it is used for; it decides what else the document declares.")]
    public WorkflowName Workflow { get; init; }

    [Required, Pattern(LabSchema.NamePattern), Description("The component's name within its service.")]
    public string Name { get; init; } = "";

    [Description("The name people read, when the name is not it.")]
    public string? DisplayName { get; init; }

    [Description("What the component is, in a sentence.")]
    public string? Description { get; init; }

    [Pattern(LabSchema.NamePattern), Description("The component of this service this one lives inside: an app's SQLite database is part of the app.")]
    public string? PartOf { get; init; }

    [Pattern(LabSchema.NamePattern, GenericParameter = 0), UniqueItems(true), Description("The components of this service this one needs, by name.")]
    public List<string>? DependsOn { get; init; }

    [UniqueItems(true), Description("The external drives it needs mounted, by mount point: a job refuses to run against one that is not, rather than write to the empty directory left in its place.")]
    public List<string>? RequiresVolumes { get; init; }
}
