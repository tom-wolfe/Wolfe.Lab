using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// Where a service serves its metrics, as a component's file writes it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record MetricsDocument
{
    [Required, Minimum(1), Maximum(65535), Description("The port the service serves its metrics on, in its own container.")]
    public int Port { get; init; }

    [Description("The path it serves them on: /metrics unless it says otherwise.")]
    public string? Path { get; init; }

    [Minimum(1), Maximum(65535), Description("The node's port to publish it on, when another service already holds the container's own number there.")]
    public int? Published { get; init; }
}
