using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A Garage node's role.
/// </summary>
[AdditionalProperties(false)]
internal sealed record LayoutDocument
{
    [Required, MinLength(1), Description("The zone the node stands in.")]
    public string Zone { get; init; } = "";

    [Required, Description("How much the node stores, as Garage spells it: 500G is decimal, 500GiB binary.")]
    public string Capacity { get; init; } = "";
}
