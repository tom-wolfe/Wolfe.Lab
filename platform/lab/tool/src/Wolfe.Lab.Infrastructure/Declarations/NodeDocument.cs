using Json.Schema.Generation;
using Wolfe.Lab.Domain.Catalog.Nodes;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A node, as <c>platform/nodes.yaml</c> writes it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record NodeDocument
{
    [Required, Const("node"), Description("A node: a machine the lab runs on, declared in platform/.")]
    public string Kind { get; init; } = "";

    [Required, Pattern(LabSchema.NamePattern), Description("The node's name: what a component's runsOn names it by.")]
    public string Name { get; init; } = "";

    [Required, Description("What the node is to the lab: what fails when it does.")]
    public NodeRole Role { get; init; }

    [Required, Description("The node's operating system and architecture, as release assets name them.")]
    public NodePlatform Platform { get; init; }

    [Required, MinLength(1), Description("Where the other nodes reach it: its name on the tailnet.")]
    public string Address { get; init; } = "";

    [Required, MinLength(1), Description("Where it installs components and their artifacts, written whole: {lab.root}, and LAB_ROOT on the node.")]
    public string Root { get; init; } = "";

    [Required, MinLength(1), Description("Where its services keep their state, written whole: {lab.data}, and LAB_DATA on the node.")]
    public string Data { get; init; } = "";

    [MinLength(1), Description("Where its Docker socket is, when it runs Docker: {node.docker}.")]
    public string? Docker { get; init; }

    [UniqueItems(true), Description("The external drives it holds, where it mounts them: {node.drives}, comma-separated.")]
    public List<string>? Drives { get; init; }
}
