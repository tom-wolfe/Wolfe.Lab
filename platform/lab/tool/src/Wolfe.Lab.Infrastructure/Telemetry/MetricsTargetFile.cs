namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// A file of metrics endpoints for a node's collector to scrape (monitoring/alloy/README.md):
/// Prometheus's file discovery format, a <see cref="DiscoveryTarget"/> per endpoint.
/// </summary>
public static class MetricsTargetFile
{
    /// <summary>
    /// The label naming the path an endpoint serves its metrics on.
    /// </summary>
    public const string PathLabel = "__metrics_path__";
}
