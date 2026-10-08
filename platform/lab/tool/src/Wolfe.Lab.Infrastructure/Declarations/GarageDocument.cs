using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component the <c>garage</c> workflow operates, as its file writes it: Garage's compose
/// service, and its node's role in the cluster.
/// </summary>
[AdditionalProperties(false)]
internal sealed record GarageDocument : DockerDocument
{
    [Required, Description("The node's role in the cluster layout, brought into line on every deploy.")]
    public LayoutDocument Layout { get; init; } = new();
}
