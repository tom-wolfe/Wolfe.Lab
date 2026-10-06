using Json.Schema.Generation;
using Wolfe.Lab.Domain.Catalog.Facets.Telemetry;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component a compose workflow (<c>docker</c>, <c>dotnet-service</c>) operates, as its file
/// writes it: one service of the compose stack beside it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record ComposeDocument : ComponentDocument
{
    [Required, Description("The compose service the component runs as, as the compose file names it: the container keeps Docker's unique name, the component the catalog's.")]
    public string Service { get; init; } = "";

    [Description("How the component's logs reach the lab, when not as its output: otlp, when it sends its own.")]
    public LogTransport? Logs { get; init; }

    [Description("Where the component serves Prometheus metrics, in its own container: an endpoint, or a list when it serves more than one.")]
    public MetricsDocuments? Metrics { get; init; }
}
