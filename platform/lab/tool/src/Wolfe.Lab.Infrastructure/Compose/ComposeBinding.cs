using Ritten.Docker;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// A compose component and the compose service it runs as, with what its declaration asks
/// of that service, as the deploy writes it into the override.
/// </summary>
/// <param name="Component">The component.</param>
/// <param name="Service">The compose service it names.</param>
/// <param name="Labels">The collector's labels its facets set, beyond where it lives.</param>
/// <param name="Publishes">The ports to publish for the collector, compose's way: <c>127.0.0.1:8081:8081</c>.</param>
public sealed record ComposeBinding(
    Component Component,
    ComposeService Service,
    IReadOnlyDictionary<ContainerLabel, string> Labels,
    IReadOnlyList<string> Publishes
)
{
    /// <summary>
    /// Where the node's collector scrapes the component's metrics: each endpoint at its port on
    /// the node's loopback.
    /// </summary>
    public IReadOnlyList<MetricsTarget> Metrics { get; init; } = [];
}
