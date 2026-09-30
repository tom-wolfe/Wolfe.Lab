namespace Wolfe.Lab.Build.Values;

/// <summary>
/// An attribute the lab's telemetry is labelled with (ROADMAP.md #11), in OpenTelemetry's
/// spelling. The one place each is named: what labels a container, a log file or a process's
/// own telemetry reads it from here.
/// </summary>
/// <remarks>
/// <c>host.name</c> and <c>lab.role</c> belong to the node, so the collector sets them
/// (monitoring/alloy), not the CLI.
/// </remarks>
public sealed record TelemetryAttribute(string Name)
{
    /// <summary>
    /// What emitted it: a container, an agent.
    /// </summary>
    public static TelemetryAttribute ServiceName { get; } = new("service.name");

    /// <summary>
    /// The area the component lives in: <c>monitoring</c>.
    /// </summary>
    public static TelemetryAttribute Area { get; } = new("lab.area");

    /// <summary>
    /// The service the component belongs to: <c>gatus</c>.
    /// </summary>
    public static TelemetryAttribute Service { get; } = new("lab.service");

    /// <summary>
    /// The component itself: <c>compose</c>.
    /// </summary>
    public static TelemetryAttribute Component { get; } = new("lab.component");

    /// <summary>
    /// How a store whose label names cannot hold a dot spells it — Loki's and Prometheus's own
    /// translation of OpenTelemetry's, so <c>service.name</c> is <c>service_name</c>.
    /// </summary>
    public string Label => Name.Replace('.', '_');

    /// <inheritdoc />
    public override string ToString() => Name;
}
